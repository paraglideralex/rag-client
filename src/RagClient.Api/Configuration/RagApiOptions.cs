namespace RagClient.Api.Configuration;

public class RagApiOptions
{
    public const string SectionName = "RagApi";

    public string BaseUrl { get; set; } = "https://rag.infra-prod.activebt.ru";
}
