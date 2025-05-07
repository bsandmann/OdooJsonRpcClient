using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PortaCapena.OdooJsonRpcClient.Consts;
using PortaCapena.OdooJsonRpcClient.Extensions;
using PortaCapena.OdooJsonRpcClient.Models;
using PortaCapena.OdooJsonRpcClient.Request;
using PortaCapena.OdooJsonRpcClient.Result;

namespace PortaCapena.OdooJsonRpcClient
{
    public sealed class OdooClient
    {
        private readonly HttpClient _client;
        public OdooConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                if (value.Timeout != TimeSpan.Zero)
                    _client.Timeout = value.Timeout;
            }
        }

        private int? _userUid;
        private OdooConfig _config;

        /// <summary>
        /// Username and Password for Basic Authentication (htaccess).
        /// Syntax: username:password
        /// </summary>
        public string BasicAuthenticationUsernamePassword
        {
            set
            {
                var byteArray = Encoding.ASCII.GetBytes(value);
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
        }

        private static HttpClient InitializeHttpClient()
        {
            var client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                MaxConnectionsPerServer = 10
            });
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public OdooClient(HttpClient client, OdooConfig config)
        {
            Config = config;
            _client = client;
        }

        public OdooClient(OdooConfig config) : this(InitializeHttpClient(), config) {}
        
        #region Get

        public Task<OdooResult<T[]>> GetAsync<T>(OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default) where T : IOdooModel, new()
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => GetAsync<T>(userUid, query, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public Task<OdooResult<T[]>> GetAsync<T>(int userUid, OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default) where T : IOdooModel, new()
        {
            return GetAsync<T>(Config, userUid, query, SelectContext(context, Config.Context), cancellationToken);
        }
        public async Task<OdooResult<T[]>> GetAsync<T>(OdooConfig odooConfig, int userUid, OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default) where T : IOdooModel, new()
        {
            var tableName = OdooExtensions.GetOdooTableName<T>();
            var request = OdooRequestModel.SearchRead(odooConfig, userUid, tableName, query, context);
            return await CallAndDeserializeAsync<T[]>(request, cancellationToken);
        }

        public Task<OdooResult<OdooDictionaryModel[]>> GetAsync(string tableName, OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => GetAsync(tableName, userUid, query, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public Task<OdooResult<OdooDictionaryModel[]>> GetAsync(string tableName, int userUid, OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return GetAsync(tableName, Config, userUid, query, SelectContext(context, Config.Context), cancellationToken);
        }
        public Task<OdooResult<OdooDictionaryModel[]>> GetAsync(string tableName, OdooConfig odooConfig, int userUid, OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.SearchRead(odooConfig, userUid, tableName, query, context);
            return CallAndDeserializeAsync<OdooDictionaryModel[]>(request, cancellationToken);
        }

        #endregion

        #region Count 

        public Task<OdooResult<long>> GetCountAsync<T>(OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default) where T : IOdooModel, new()
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => GetCountAsync<T>(userUid, query, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public Task<OdooResult<long>> GetCountAsync<T>(int userUid, OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default) where T : IOdooModel, new()
        {
            return GetCountAsync<T>(Config, userUid, query, SelectContext(context, Config.Context), cancellationToken);
        }
        public async Task<OdooResult<long>> GetCountAsync<T>(OdooConfig odooConfig, int userUid, OdooQuery query = null, OdooContext context = null, CancellationToken cancellationToken = default) where T : IOdooModel, new()
        {
            var tableName = OdooExtensions.GetOdooTableName<T>();
            var request = OdooRequestModel.SearchCount(odooConfig, userUid, tableName, query, context);
            return await CallAndDeserializeAsync<long>(request, cancellationToken);
        }

        #endregion

        #region Create

        public Task<OdooResult<long>> CreateAsync(IOdooCreateModel model, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => CreateAsync(Config, userUid, model, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public async Task<OdooResult<long>> CreateAsync(OdooConfig odooConfig, int userUid, IOdooCreateModel model, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Create(odooConfig, userUid, model.OdooTableName(), model, context);
            var result = await CallAndDeserializeAsync<long>(request, cancellationToken);
            return result.Succeed ? result.ToResult(result.Value) : OdooResult<long>.FailedResult(result);
        }
        public Task<OdooResult<long>> CreateAsync(OdooDictionaryModel model, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => CreateAsync(Config, userUid, model, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public async Task<OdooResult<long>> CreateAsync(OdooConfig odooConfig, int userUid, OdooDictionaryModel model, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Create(odooConfig, userUid, GetTableName(model), model, context);
            var result = await CallAndDeserializeAsync<long>(request, cancellationToken);
            return result.Succeed ? result.ToResult(result.Value) : OdooResult<long>.FailedResult(result);
        }

        public Task<OdooResult<long[]>> CreateAsync(IEnumerable<IOdooCreateModel> models, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => CreateAsync(Config, userUid, models, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public async Task<OdooResult<long[]>> CreateAsync(OdooConfig odooConfig, int userUid, IEnumerable<IOdooCreateModel> models, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Create(odooConfig, userUid, models.First().OdooTableName(), models, context);
            var result = await CallAndDeserializeAsync<long[]>(request, cancellationToken);
            return result.Succeed ? result.ToResult(result.Value) : OdooResult<long[]>.FailedResult(result);
        }
        public Task<OdooResult<long[]>> CreateAsync(IEnumerable<OdooDictionaryModel> models, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => CreateAsync(Config, userUid, models, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public async Task<OdooResult<long[]>> CreateAsync(OdooConfig odooConfig, int userUid, IEnumerable<OdooDictionaryModel> models, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Create(odooConfig, userUid, GetTableName(models.First()), models, context);
            var result = await CallAndDeserializeAsync<long[]>(request, cancellationToken);
            return result.Succeed ? result.ToResult(result.Value) : OdooResult<long[]>.FailedResult(result);
        }
        public Task<OdooResult<object>> ActionAsync(string tableName, string action, object param, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => ActionAsync(Config, userUid, tableName, action, param, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public async Task<OdooResult<object>> ActionAsync(OdooConfig odooConfig, int userUid, string tableName, string action, object model,
                                                          OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Action(odooConfig, userUid, tableName, action, model, context);
            var result = await CallAndDeserializeAsync<object>(request, cancellationToken);
            return result.Succeed ? result.ToResult(result.Value) : OdooResult<object>.FailedResult(result);
        }
        public Task<OdooResult<object>> ActionCollectionAsync(string tableName, string action, object param, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => ActionCollectionAsync(Config, userUid, tableName, action, param, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public async Task<OdooResult<object>> ActionCollectionAsync(OdooConfig odooConfig, int userUid, string tableName, string action, object model,
                                                                    OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.ActionCollection(odooConfig, userUid, tableName, action, model, context);
            var result = await CallAndDeserializeAsync<object>(request, cancellationToken);
            return result.Succeed ? result.ToResult(result.Value) : OdooResult<object>.FailedResult(result);
        }

        #endregion

        #region  Update

        public Task<OdooResult<bool>> UpdateAsync(IOdooCreateModel model, long id, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return UpdateRangeAsync(model, new[] { id }, SelectContext(context, Config.Context), cancellationToken);
        }
        public Task<OdooResult<bool>> UpdateAsync(OdooConfig odooConfig, int userUid, IOdooCreateModel model, long id, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return UpdateRangeAsync(odooConfig, userUid, model, new[] { id }, context, cancellationToken);
        }
        public Task<OdooResult<bool>> UpdateAsync(OdooDictionaryModel model, long id, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return UpdateRangeAsync(model, new[] { id }, SelectContext(context, Config.Context), cancellationToken);
        }
        public Task<OdooResult<bool>> UpdateAsync(OdooConfig odooConfig, int userUid, OdooDictionaryModel model, long id, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return UpdateRangeAsync(odooConfig, userUid, model, new[] { id }, context, cancellationToken);
        }

        public Task<OdooResult<bool>> UpdateRangeAsync(IOdooCreateModel model, long[] ids, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => UpdateRangeAsync(Config, userUid, model, ids, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public async Task<OdooResult<bool>> UpdateRangeAsync(OdooConfig odooConfig, int userUid, IOdooCreateModel model, long[] ids, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var tableName = model.OdooTableName();
            var request = OdooRequestModel.Update(odooConfig, userUid, tableName, ids, model, context);
            return await CallAndDeserializeAsync<bool>(request, cancellationToken);
        }
        public Task<OdooResult<bool>> UpdateRangeAsync(OdooDictionaryModel model, long[] ids, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => UpdateRangeAsync(Config, userUid, model, ids, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public Task<OdooResult<bool>> UpdateRangeAsync(OdooConfig odooConfig, int userUid, OdooDictionaryModel model, long[] ids, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Update(odooConfig, userUid, GetTableName(model), ids, model, context);
            return CallAndDeserializeAsync<bool>(request, cancellationToken);
        }

        #endregion

        #region  Delete

        public Task<OdooResult<bool>> DeleteAsync(IOdooModel model, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return DeleteAsync(model.OdooTableName(), model.Id, SelectContext(context, Config.Context), cancellationToken);
        }
        public Task<OdooResult<bool>> DeleteAsync(string tableName, long id, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return DeleteRangeAsync(tableName, new[] { id }, SelectContext(context, Config.Context), cancellationToken);
        }
        public Task<OdooResult<bool>> DeleteAsync(OdooConfig odooConfig, int userUid, string tableName, long id, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return DeleteRangeAsync(odooConfig, userUid, tableName, new[] { id }, context, cancellationToken);
        }

        public Task<OdooResult<bool>> DeleteRangeAsync(IOdooModel[] models, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var tableName = models.First().OdooTableName();
            if (models.Any(x => !string.Equals(x.OdooTableName(), tableName)))
                throw new ArgumentException("Models are not from the same odoo table");

            return DeleteRangeAsync(tableName, models.Select(x => x.Id).ToArray(), SelectContext(context, Config.Context), cancellationToken);
        }
        public Task<OdooResult<bool>> DeleteRangeAsync(string tableName, long[] ids, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => DeleteRangeAsync(Config, userUid, tableName, ids, SelectContext(context, Config.Context), cancellationToken), cancellationToken);
        }
        public Task<OdooResult<bool>> DeleteRangeAsync(OdooConfig odooConfig, int userUid, string tableName, long[] ids, OdooContext context = null, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Delete(odooConfig, userUid, tableName, ids, context);
            return CallAndDeserializeAsync<bool>(request, cancellationToken);
        }

        #endregion

        #region Login

        public Task<OdooResult<int>> GetCurrentUserUidOrLoginAsync(CancellationToken cancellationToken = default)
        {
            if (_userUid.HasValue)
                return Task.FromResult(OdooResult<int>.SucceedResult(_userUid.Value));

            return LoginAsync(cancellationToken);
        }
        public async Task<OdooResult<int>> LoginAsync(CancellationToken cancellationToken = default)
        {
            var result = await LoginAsync(Config, cancellationToken);

            if (result.Succeed)
                _userUid = result.Value;

            return result;
        }
        public Task<OdooResult<int>> LoginAsync(OdooConfig odooConfig, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Login(odooConfig);
            return CallAndDeserializeAsync<int>(request, cancellationToken);
        }

        #endregion

        #region Build C# Model

        public Task<OdooResult<Dictionary<string, OdooPropertyInfo>>> GetModelAsync(string tableName)
        {
            return ExecuteWithAccessDeniedRetryAsync(userUid => GetModelAsync(Config, userUid, tableName));
        }
        public Task<OdooResult<Dictionary<string, OdooPropertyInfo>>> GetModelAsync(OdooConfig odooConfig, int userUid, string tableName, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.ModelFields(odooConfig, userUid, tableName);
            return CallAndDeserializeAsync<Dictionary<string, OdooPropertyInfo>>(request, cancellationToken);
        }

        #endregion

        #region Version

        public Task<OdooResult<OdooVersion>> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            return GetVersionAsync(Config, cancellationToken);
        }
        public Task<OdooResult<OdooVersion>> GetVersionAsync(OdooConfig odooConfig, CancellationToken cancellationToken = default)
        {
            var request = OdooRequestModel.Version(odooConfig);
            return CallAndDeserializeAsync<OdooVersion>(request, cancellationToken);
        }

        #endregion

        private async Task<OdooResult<TResult>> ExecuteWithAccessDeniedRetryAsync<TResult>(Func<int, Task<OdooResult<TResult>>> func, CancellationToken cancellationToken = default)
        {
            var userUid = await GetCurrentUserUidOrLoginAsync(cancellationToken);
            if (userUid.Failed)
                return OdooResult<TResult>.FailedResult(userUid);

            var result = await func.Invoke(userUid.Value);

            if (!result.Failed || !string.Equals(result.Error?.Data?.Name, OdooExceptionName.AccessDenied))
                return result;

            var loginUid = await LoginAsync(cancellationToken);
            if (loginUid.Failed)
                return OdooResult<TResult>.FailedResult(loginUid);

            return await func.Invoke(loginUid.Value);
        }
        public async Task<OdooResult<T>> CallAndDeserializeAsync<T>(OdooRequestModel request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await CallAsync(request, cancellationToken);
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<OdooResult<T>>(responseString);
                return result;
            }
            catch (Exception e)
            {
                return OdooResult<T>.FailedResult(e.ToString());
            }
        }
        public async Task<HttpResponseMessage> CallAsync(OdooRequestModel requestModel, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(requestModel, new IsoDateTimeConverter { DateTimeFormat = OdooConsts.DateTimeFormat });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var result = await _client.PostAsync(requestModel.Params.Url, data, cancellationToken);
            return result;
        }

        private static string GetTableName(OdooDictionaryModel model)
        {
            return model?.TableName 
                ?? throw new ArgumentException($"TableName not set in {nameof(OdooDictionaryModel)}");
        }

        private static OdooContext SelectContext(OdooContext paramContext, OdooContext mainContext)
        {
            return paramContext ?? mainContext;
        }
    }
}
