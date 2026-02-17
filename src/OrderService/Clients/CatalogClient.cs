using System.Net;
using System.Net.Http.Json;
using Shared.Contracts;

namespace OrderService.Clients;

public sealed class CatalogClient(HttpClient httpClient)
{
    public async Task<CatalogItem?> GetItem(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"items/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CatalogItem>(cancellationToken: cancellationToken);
    }
}

