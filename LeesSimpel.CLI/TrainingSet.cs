using NiceIO;

static class TrainingSet
{
    public static NPath BackendDirectory { get; set; } = NPath.CurrentDirectory.ParentContaining("leessimpel-backend").Combine("leessimpel-backend");
    public static NPath Directory => BackendDirectory.Parent.Combine("leessimpel-trainingset");
}