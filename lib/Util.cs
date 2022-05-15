namespace Lib;

public static class Util {
    // Consider Task.Run(() => SomeMethod()); 
    // If SomeMethod is
    // async Task SomeMethod()
    // then the exceptions thrown by it will just... disappear
    // If SomeMethod is
    // async void SomeMethod()
    // then the exceptions thrown by it 
    // will cause a top-level exception and stop the program
    // 
    // This TaskRunSafe method will always cause a top-level exception
    public static void TaskRunSafe(Func<Task> spawner) {
        Task.Run(() => TopLevelTaskWrapper(spawner));
    }

    private static async void TopLevelTaskWrapper(Func<Task> spawner) {
        await spawner();
    }
}
