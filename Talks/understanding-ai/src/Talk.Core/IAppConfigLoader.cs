using Microsoft.Extensions.Configuration;

namespace Talk.Core;

public interface IAppConfigLoader
{
    AppConfig Load(IConfiguration configuration);
}
