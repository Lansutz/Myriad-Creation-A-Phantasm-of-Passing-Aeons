param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('openalex','crossref','semantic','arxiv','pubmed')]
    [string]$Engine,

    [Parameter(Mandatory = $true)]
    [string]$Query,

    [int]$Limit = 10
)

$ErrorActionPreference = 'Stop'
$encoded = [uri]::EscapeDataString($Query)

function New-Result($title, $year, $doi, $url, $abstract) {
    [pscustomobject]@{
        title    = $title
        year     = $year
        doi      = $doi
        url      = $url
        abstract = $abstract
    }
}

try {
    switch ($Engine) {
        'openalex' {
            $uri = "https://api.openalex.org/works?search=$encoded&per-page=$Limit&select=id,title,publication_year,doi"
            $data = Invoke-RestMethod -Uri $uri -Headers @{ 'User-Agent' = 'myriad-search/1.0 (mailto:local@example.invalid)' }
            if ($data -is [string]) { $data = $data | ConvertFrom-Json }
            foreach ($w in $data.results) {
                New-Result $w.title $w.publication_year $w.doi $w.id $null
            }
        }
        'crossref' {
            $uri = "https://api.crossref.org/works?query=$encoded&rows=$Limit"
            $data = Invoke-RestMethod -Uri $uri -Headers @{ 'User-Agent' = 'myriad-search/1.0 (mailto:local@example.invalid)' }
            foreach ($w in $data.message.items) {
                $year = $null
                if ($w.published -and $w.published.'date-parts') { $year = $w.published.'date-parts'[0][0] }
                New-Result ($w.title -join ' ') $year $w.DOI $w.URL $null
            }
        }
        'semantic' {
            $fields = 'title,year,authors,abstract,externalIds,url'
            $uri = "https://api.semanticscholar.org/graph/v1/paper/search?query=$encoded&limit=$Limit&fields=$fields"
            $data = Invoke-RestMethod -Uri $uri
            foreach ($p in $data.data) {
                $doi = $null
                if ($p.externalIds) { $doi = $p.externalIds.DOI }
                New-Result $p.title $p.year $doi $p.url $p.abstract
            }
        }
        'arxiv' {
            $uri = "http://export.arxiv.org/api/query?search_query=all:$encoded&max_results=$Limit"
            [xml]$data = Invoke-WebRequest -Uri $uri -UseBasicParsing | Select-Object -ExpandProperty Content
            foreach ($e in $data.feed.entry) {
                New-Result $e.title $e.published.Substring(0, 4) $null $e.id $e.summary
            }
        }
        'pubmed' {
            $search = Invoke-RestMethod -Uri "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed&term=$encoded&retmode=json&retmax=$Limit"
            $ids = $search.esearchresult.idlist
            if ($ids.Count -gt 0) {
                $summary = Invoke-RestMethod -Uri "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esummary.fcgi?db=pubmed&id=$($ids -join ',')&retmode=json"
                foreach ($id in $ids) {
                    $item = $summary.result.$id
                    New-Result $item.title $item.pubdate $null "https://pubmed.ncbi.nlm.nih.gov/$id/" $null
                }
            }
        }
    }
}
catch {
    Write-Error "search failed: $($_.Exception.Message)"
    exit 1
}


