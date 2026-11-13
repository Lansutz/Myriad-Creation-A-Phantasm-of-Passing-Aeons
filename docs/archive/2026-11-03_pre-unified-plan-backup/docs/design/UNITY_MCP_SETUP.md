# Unity 编辑器接入（@opdsh/unity-plugin）

当前使用 **@opdsh/unity-plugin**，通过 Unity Pipeline 控制编辑器。

## 已安装

- DSH 插件：`@opdsh/unity-plugin@0.1.7`
- Unity CLI：`1.0.0-beta.1`
  - 位置：`C:\Users\25652\AppData\Local\Unity\bin\unity.exe`
- Unity 包：`com.unity.pipeline@0.9.1-exp.1`
- 旧的 `dsh-unity-mcp` 与 `com.coplaydev.unity-mcp` 已移除

## 为什么需要 wrapper

`unity` CLI 的 `status` / `command` 在当前机器上会挂起，但 Unity Pipeline 的 HTTP API 正常：

```text
http://127.0.0.1:7800/api/status
http://127.0.0.1:7800/api/commands
http://127.0.0.1:7800/api/exec
```

因此使用 [_tools/unity-cli-wrapper/unity.exe](_tools/unity-cli-wrapper/unity.exe)：

- `status`：直接探测 Pipeline HTTP，返回 CLI JSON 信封；
- `command`：把命令转成 `/api/exec` 请求；
- 其他参数：转发给真实 `unity.exe`。

profile `cordis.patch.yml` 已指向 wrapper：

```yaml
- id: unity
  config:
    warmShell: false
    unityBin: 'D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\_tools\unity-cli-wrapper\unity.exe'
    projectPath: 'D:\Myriad-Creation-A-Phantasm-of-Passing-Aeons\MyriadCreation'
```

## 启动

1. 用 `-automated` 打开 Unity 工程；
2. 确认 Pipeline 描述文件存在：
   `MyriadCreation/Library/Pipeline/.unity-pipeline-port`
3. 验证：

```powershell
_tools/unity-cli-wrapper/unity.exe status
_tools/unity-cli-wrapper/unity.exe command "scene.get_hierarchy"
```

4. 重启 DSH 会话，让 `@opdsh/unity-plugin` 注册工具；
5. 在会话里调用 `unity_status`，应返回 `count: 1`。

## 工具

- `unity_status`
- `unity_list_commands`
- `unity_command`
- `unity_eval`
- `unity_cli`

`unity_command` 命令名使用 `group.command`，如 `scene.get_hierarchy`；
wrapper 也接受 `scene get_hierarchy` 形式。
