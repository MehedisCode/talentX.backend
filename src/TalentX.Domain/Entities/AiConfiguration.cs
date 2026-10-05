using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class AiConfiguration : BaseEntity
{
    public string ModelName { get; private set; } = string.Empty;
    public string? EncryptedApiKey { get; private set; }

    private AiConfiguration() : base()
    {
    }

    public AiConfiguration(string modelName, string? encryptedApiKey = null)
    {
        ModelName = modelName;
        EncryptedApiKey = encryptedApiKey;
    }

    public void Update(string modelName, string? encryptedApiKey)
    {
        ModelName = modelName;
        if (!string.IsNullOrWhiteSpace(encryptedApiKey))
        {
            EncryptedApiKey = encryptedApiKey;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
