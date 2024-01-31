//-----------------------------------------------------------------------
// <copyright file="AddressValidationService.cs" company="Procare Software, LLC">
//     Copyright © 2021-2024 Procare Software, LLC. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Procare.AddressValidation.Tester;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class AddressValidationService : BaseHttpService
{
    public AddressValidationService(IHttpClientFactory httpClientFactory, bool disposeFactory, Uri baseUrl)
        : this(httpClientFactory, disposeFactory, baseUrl, null, false)
    {
    }

    protected AddressValidationService(IHttpClientFactory httpClientFactory, bool disposeFactory, Uri baseUrl, HttpMessageHandler? httpMessageHandler, bool disposeHandler)
        : base(httpClientFactory, disposeFactory, baseUrl, httpMessageHandler, disposeHandler)
    {
    }

    public async Task<string> GetAddressesAsync(AddressValidationRequest request, int attempts = 0)
    {
        // I wanted to use a retry policy but I wasn't sure how to implement that in a console application so I just did it manually
        await Task.Delay((int)Math.Pow(2, attempts)).ConfigureAwait(false);

        if (attempts > 3)
        {
            // the call has failed 3 times, time to abort
            throw new ArgumentException("Address lookup has failed too many times and has been aborted");
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(750);
        var token = cancellationTokenSource.Token;

        try
        {
            using var httpRequest = request.ToHttpRequest(this.BaseUrl);
            using var response = await this.CreateClient().SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

            if ((int)response.StatusCode >= 500)
            {
                // call got a 5xx response, retry and increment attempts counter
                return await this.GetAddressesAsync(request, attempts + 1).ConfigureAwait(false);
            }
            else
            {
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // call took to long and got cancelled, retry and increment attempts counter
            return await this.GetAddressesAsync(request, attempts + 1).ConfigureAwait(false);
        }
    }
}
