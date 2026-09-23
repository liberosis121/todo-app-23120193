using TodoApp.UI;

namespace TodoApp.Tests;

public class ConsoleUiTests
{
    [Fact]
    public void GetTaskPromptPageSize_AlwaysSatisfiesSpectreRange()
    {
        Assert.Equal(3, ConsoleUi.GetTaskPromptPageSize(1));
        Assert.Equal(3, ConsoleUi.GetTaskPromptPageSize(2));
        Assert.Equal(3, ConsoleUi.GetTaskPromptPageSize(3));
        Assert.Equal(7, ConsoleUi.GetTaskPromptPageSize(7));
        Assert.Equal(10, ConsoleUi.GetTaskPromptPageSize(10));
        Assert.Equal(10, ConsoleUi.GetTaskPromptPageSize(100));
    }
}
