using System;
using System.Threading;
using System.Threading.Tasks;

public static class TaskExtensions
{
    public static async void FireAndForgetAsync(this Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            if (!task.IsCanceled)
                throw;
        }
    }

    public static async Task WaitUntilAsync(Func<bool> predicate, CancellationToken token = default)
    {
        try
        {
            while (!predicate() && !token.IsCancellationRequested)
                await Task.Yield();
        }
        catch (NullReferenceException)
        {
            throw;
        }
    }

    public static async Task WaitWhileAsync(Func<bool> predicate, CancellationToken token = default)
    {
        try
        {
            while (predicate() && !token.IsCancellationRequested)
                await Task.Yield();
        }
        catch (NullReferenceException)
        {
            throw;
        }
    }

    public static async Task DelayActionAsync(Action action, int milliseconds, CancellationToken token = default)
    {
        try
        {
            await Task.Delay(milliseconds, token);
            action();
        }
        catch (NullReferenceException)
        {
            throw;
        }
    }

    public static async Task DelayActionFramesAsync(Action action, int frames, CancellationToken token = default)
    {
        try
        {
            for (int i = 0; i < frames && !token.IsCancellationRequested; i++)
                await Task.Yield();
            action();
        }
        catch
        {
            throw;
        }
    }
}
