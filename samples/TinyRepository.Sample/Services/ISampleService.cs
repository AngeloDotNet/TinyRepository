using TinyRepository.Sample.Entities;

namespace TinyRepository.Sample.Services;

public interface ISampleService
{
    //Task<SampleEntity?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<SampleEntity?> GetByIdAsync(int id, string[]? include, CancellationToken cancellationToken);
    Task<SampleEntity> CreateAsync(SampleEntity entity, CancellationToken cancellationToken);
}