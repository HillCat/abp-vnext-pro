using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using Volo.Abp.Caching;
using Volo.Abp.Settings;

namespace Lion.AbpPro;

[DependsOn(
    typeof(AbpSwashbuckleModule),
    typeof(AbpAutofacModule),
    typeof(LionAbpProCoreModule))]
public class SharedHostingMicroserviceModule : AbpModule
{
    private const string DefaultCorsPolicyName = "Default";

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddConsulConfig(configuration);
        ConfigureHealthChecks(context);
        ConfigureLocalization();
        ConfigureCors(context);
        ConfigureConsul(context, configuration);
        ConfigAntiForgery();
        ConfigureAbpExceptions(context);
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // 设置默认语言为简体中文
        context.ServiceProvider.GetRequiredService<ISettingDefinitionManager>().Get(LocalizationSettingNames.DefaultLanguage).DefaultValue = "zh-Hans";  
    }

    /// <summary>
    /// 异常处理
    /// </summary>
    private void ConfigureAbpExceptions(ServiceConfigurationContext context)
    {
        context.Services.AddMvc
        (
            options =>
            {
                options.Filters.Add(typeof(LionExceptionFilter));
                options.Filters.Add(typeof(LionResultFilter));
            }
        );
    }

    /// <summary>
    /// 阻止跨站点请求伪造
    /// https://docs.microsoft.com/zh-cn/aspnet/core/security/anti-request-forgery?view=aspnetcore-6.0
    /// </summary>
    private void ConfigAntiForgery()
    {
        Configure<AbpAntiForgeryOptions>(options => { options.AutoValidate = false; });
    }

    private void ConfigureConsul(ServiceConfigurationContext context,IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("Consul:Enabled", false))
        {
            context.Services.AddConsulConfig(configuration);
        }
    }


    /// <summary>
    /// 配置跨域
    /// </summary>
    private void ConfigureCors(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicyName, builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    // https://www.cnblogs.com/JulianHuang/p/14225515.html
                    // https://learn.microsoft.com/zh-cn/aspnet/core/security/cors?view=aspnetcore-7.0
                    .SetPreflightMaxAge((TimeSpan.FromHours(24)));
            });
        });
    }



    /// <summary>
    /// 多语言配置
    /// </summary>
    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Languages.Add(new LanguageInfo("ar", "ar", "العربية"));
            options.Languages.Add(new LanguageInfo("cs", "cs", "Čeština"));
            options.Languages.Add(new LanguageInfo("en", "en", "English"));
            options.Languages.Add(new LanguageInfo("en-GB", "en-GB", "English (UK)"));
            options.Languages.Add(new LanguageInfo("fr", "fr", "Français"));
            options.Languages.Add(new LanguageInfo("hu", "hu", "Magyar"));
            options.Languages.Add(new LanguageInfo("pt-BR", "pt-BR", "Português"));
            options.Languages.Add(new LanguageInfo("ru", "ru", "Русский"));
            options.Languages.Add(new LanguageInfo("tr", "tr", "Türkçe"));
            options.Languages.Add(new LanguageInfo("zh-Hans", "zh-Hans", "简体中文"));
            options.Languages.Add(new LanguageInfo("zh-Hant", "zh-Hant", "繁體中文"));
            options.Languages.Add(new LanguageInfo("de-DE", "de-DE", "Deutsch", "de"));
            options.Languages.Add(new LanguageInfo("es", "es", "Español", "es"));
        });
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    private void ConfigureHealthChecks(ServiceConfigurationContext context)
    {
        // TODO 检查数据库和redis是否正常 AspNetCore.HealthChecks.Redis AspNetCore.HealthChecks.MySql
        // context.Services.AddHealthChecks().AddRedis(redisConnectionString).AddMySql(connectString);
        context.Services.AddHealthChecks();
    }
}