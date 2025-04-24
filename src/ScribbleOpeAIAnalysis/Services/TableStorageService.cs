using Azure.Data.Tables;
using ScribbleOpeAIAnalysis.Models;
using System.Text.Json;

namespace ScribbleOpeAIAnalysis.Services
{
    public class TableStorageService : ITableStorageService
    {
        private readonly TableClient _tableClient;
        private const string TableName = "AnalysisResults";

        public TableStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["Storage:Azure:ConnectionString"];
            _tableClient = new TableClient(connectionString, TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<AnalysisResult> GetAnalysisResultAsync(string guid)
        {
            try
            {
                return await _tableClient.GetEntityAsync<AnalysisResult>("Analysis", guid);
            }
            catch (Azure.RequestFailedException)
            {
                return null;
            }
        }

        public async Task UpsertAnalysisResultAsync(string guid, Dictionary<string, object> changes)
        {
            var result = await GetAnalysisResultAsync(guid) ?? new AnalysisResult { RowKey = guid };

            foreach (var change in changes)
            {
                var prop = typeof(AnalysisResult).GetProperty(change.Key);
                if (prop != null)
                {
                    if (change.Key == "Component" && change.Value is List<string> list)
                    {
                        result.SetComponentList(list);
                    }
                    else
                    {
                        prop.SetValue(result, change.Value);
                    }
                }
            }

            await _tableClient.UpsertEntityAsync(result);
        }

        public async Task<bool> DeleteAnalysisResultAsync(string guid)
        {
            try
            {
                await _tableClient.DeleteEntityAsync("Analysis", guid);
                return true;
            }
            catch (Azure.RequestFailedException)
            {
                return false;
            }
        }
    }
}
