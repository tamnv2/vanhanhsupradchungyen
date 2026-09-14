using Vhdchy.LanService;

namespace Vhdchy.LanSlice1Business.Harness;

internal static class HarnessAssert
{
    internal static void That(bool condition, string code)
    {
        if (!condition) throw new InvalidOperationException(code);
    }

    internal static async Task ErrorAsync(string code, Func<Task> action)
    {
        try
        {
            await action();
            throw new InvalidOperationException($"{code}_NOT_REJECTED");
        }
        catch (Slice1BusinessException error) when (error.Code == code)
        {
        }
        catch (LanLocalCommandException error) when (error.Code == code)
        {
        }
    }
}
