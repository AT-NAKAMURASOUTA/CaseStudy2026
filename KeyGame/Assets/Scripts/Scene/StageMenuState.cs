public static class StageMenuState
{
    public static SCENETYPE RestartStage { get; private set; } = SCENETYPE.STAGE1;

    public static void SetRestartStage(SCENETYPE stageType)
    {
        RestartStage = stageType;
    }
}
