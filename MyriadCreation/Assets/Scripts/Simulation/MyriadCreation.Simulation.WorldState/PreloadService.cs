using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.WorldState
{
public struct PreloadReport
{
	public int tasksCompleted;

	public int tasksDeferred;

	public double elapsedMs;

	public int framesUsed;

	public bool allDone;
}

/// <summary>
/// D39 预加载/预热：把内容表、AI 控制器/上下文、常用查询拆成可预算的任务，
/// 主线程按帧执行；超出预算的任务留到下一帧，不阻塞首帧、不跨线程碰 Unity 状态。
/// 任务必须幂等：重复执行只保证"已就绪"，不产生玩法副作用。
/// </summary>
public sealed class PreloadService
{
	private readonly Queue<PreloadTask> _queue = new Queue<PreloadTask>();

	private int _framesUsed;

	public int TasksCompleted { get; private set; }

	public int PendingCount => _queue.Count;

	public bool IsIdle => _queue.Count == 0;

	public PreloadReport LastReport { get; private set; }

	public void Clear()
	{
		_queue.Clear();
		TasksCompleted = 0;
		_framesUsed = 0;
		LastReport = default;
	}

	public void Enqueue(string id, Func<bool> run)
	{
		if (run != null)
		{
			_queue.Enqueue(new PreloadTask
			{
				id = id ?? "",
				run = run
			});
		}
	}

	/// <summary>
	/// 在给定预算内执行队首任务；budgetMs &lt;= 0 表示不限预算。
	/// 时钟以毫秒为单位，测试可注入确定性时钟。
	/// </summary>
	public PreloadReport RunBudgeted(double budgetMs, Func<double> clockMs)
	{
		double start = (clockMs != null) ? clockMs() : 0.0;
		int completed = 0;
		while (_queue.Count > 0)
		{
			double now = (clockMs != null) ? clockMs() : 0.0;
			if (budgetMs > 0.0 && now - start >= budgetMs)
			{
				break;
			}
			PreloadTask task = _queue.Dequeue();
			task.run();
			completed++;
			TasksCompleted++;
		}
		_framesUsed++;
		LastReport = new PreloadReport
		{
			tasksCompleted = completed,
			tasksDeferred = _queue.Count,
			elapsedMs = (clockMs != null) ? clockMs() - start : 0.0,
			framesUsed = _framesUsed,
			allDone = _queue.Count == 0
		};
		return LastReport;
	}

	private struct PreloadTask
	{
		public string id;

		public Func<bool> run;
	}
}
}
