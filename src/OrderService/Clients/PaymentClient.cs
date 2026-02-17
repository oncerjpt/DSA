using System.Net;
using System.Net.Http.Json;
using Shared.Contracts;

namespace OrderService.Clients;

public sealed class PaymentClient(HttpClient httpClient)
{
    public async Task<Payment?> GetPayment(Guid id, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"payments/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Payment>(cancellationToken: cancellationToken);
    }

    public async Task<Payment> Authorize(PaymentRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "payments")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payment = await response.Content.ReadFromJsonAsync<Payment>(cancellationToken: cancellationToken);
        return payment ?? throw new InvalidOperationException("Payment service returned an empty response.");
    }
}

