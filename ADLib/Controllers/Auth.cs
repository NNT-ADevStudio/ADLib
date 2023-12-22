using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADLib.Controllers
{
    internal class Auth
    {
        public string ClientId { get; }

        public string TenantId { get; }

        public string AccessToken { get; private set; }

        private readonly string[] scopes = new string[] { "user.read" };

        readonly IPublicClientApplication app;

        public Auth(string clientId, string tenantId, string redict = "http://localhost")
        {
            ClientId = clientId;
            TenantId = tenantId;

            app = PublicClientApplicationBuilder.Create(ClientId)
               .WithRedirectUri(redict)
               .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
               .WithDefaultRedirectUri()
               .Build();
        }

        public void CreateCacheHelper()
        {
            string cacheFilePath =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    $"msal_cache_{AppDomain.CurrentDomain}.dat");

            var cacheHelper = new TokenCacheHelper(cacheFilePath);
            app.UserTokenCache.SetBeforeAccess(cacheHelper.BeforeAccessNotification);
            app.UserTokenCache.SetAfterAccess(cacheHelper.AfterAccessNotification);
            app.UserTokenCache.SetBeforeWrite(cacheHelper.BeforeWriteNotification);
        }

        public void CreateCacheHelper(string datPath)
        {
            var cacheHelper = new TokenCacheHelper(datPath);
            app.UserTokenCache.SetBeforeAccess(cacheHelper.BeforeAccessNotification);
            app.UserTokenCache.SetAfterAccess(cacheHelper.AfterAccessNotification);
            app.UserTokenCache.SetBeforeWrite(cacheHelper.BeforeWriteNotification);
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                // Если в кэше есть аккаунт, попытаемся получить токен без интерактивного входа
                var silentResult = await app.AcquireTokenSilent(scopes, firstAccount)
                                            .ExecuteAsync();
                AccessToken = silentResult.AccessToken;
                return silentResult.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                // Если не удалось получить токен немого, запрашиваем интерактивный вход
                try
                {
                    var interactiveResult = await app.AcquireTokenInteractive(scopes)
                                                     .WithAccount(firstAccount)
                                                     .ExecuteAsync();
                    AccessToken = interactiveResult.AccessToken;
                    return interactiveResult.AccessToken;
                }
                catch
                {
                    try
                    {
                        var result = app.AcquireTokenInteractive(scopes).ExecuteAsync().Result;
                        AccessToken = result.AccessToken;
                        return result.AccessToken;
                    }
                    catch (MsalClientException exmsal)
                    {
                        Debug.WriteLine("Ошибка при аутентификации: " + exmsal.Message);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
