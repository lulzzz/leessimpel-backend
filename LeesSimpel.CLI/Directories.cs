using NiceIO;

static class Directories
{
    public static NPath Backend { get; } = NPath.CurrentDirectory.ParentContaining("leessimpel-backend").Combine("leessimpel-backend");
    public static NPath TrainingSet => Backend.Parent.Combine("leessimpel-trainingset");
}