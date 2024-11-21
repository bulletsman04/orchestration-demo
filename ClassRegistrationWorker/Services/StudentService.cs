namespace ClassRegistrationWorker.Services;

public class StudentService : IStudentService
{
    public Task<bool> IsStudentActive(Guid studentId)
    {
        // in real app: call to student service
        
        return Task.FromResult(true);
    }
}

public interface IStudentService
{
    Task<bool> IsStudentActive(Guid studentId);
}