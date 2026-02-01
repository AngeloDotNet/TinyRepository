using Microsoft.EntityFrameworkCore;
using TinyRepository.Interfaces;
using TinyRepository.Sample.Entities;

namespace TinyRepository.Sample.Services;

public class SampleService(IRepository<SampleEntity, int> repository, IUnitOfWork unitOfWork) : ISampleService
{
    //public async Task<SampleEntity?> GetByIdAsync(int id, CancellationToken cancellationToken)
    //    => await repository.GetByIdAsync(id, cancellationToken);

    public async Task<SampleEntity?> GetByIdAsync(int id, string[]? include, CancellationToken cancellationToken)
    {
        if (include is null || include.Length == 0)
        {
            return await repository.GetByIdAsync(id, cancellationToken);
        }

        var result = await repository.Query(true, include).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return result;
    }

    public async Task<SampleEntity> CreateAsync(SampleEntity entity, CancellationToken cancellationToken)
    {
        var newEntity = new SampleEntity()
        {
            Name = entity.Name,
            CreatedAt = DateTime.UtcNow,
            Author = new Author
            {
                FirstName = entity.Author?.FirstName,
                LastName = entity.Author?.LastName
            }
        };

        await repository.AddAsync(newEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newEntity;
    }
}