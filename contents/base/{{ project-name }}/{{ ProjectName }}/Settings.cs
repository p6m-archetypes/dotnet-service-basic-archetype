namespace {{ ProjectName }};

public class Settings
{
    public string Host { get; init; } = "0.0.0.0";
    public int Port { get; init; } = {{ service_port }};
    public int ManagementPort { get; init; } = {{ management_port }};
}
