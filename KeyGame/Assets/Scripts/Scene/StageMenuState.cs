public static class StageMenuState
{
    public static SCENETYPE RestartStage { get; private set; } = SCENETYPE.STAGE1_1;

    public static void SetRestartStage(SCENETYPE stageType)
    {
        RestartStage = stageType;
    }
}
