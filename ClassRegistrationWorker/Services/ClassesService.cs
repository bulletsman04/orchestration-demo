namespace ClassRegistrationWorker.Services;

public class ClassesService : IClassesService
{
    public Task<bool> IsClassActiveForRegistration(Guid classId)
    {
        // in real app: call to classes service

        return Task.FromResult(true);
    }
}

public interface IClassesService
{
    Task<bool> IsClassActiveForRegistration(Guid classId);
}