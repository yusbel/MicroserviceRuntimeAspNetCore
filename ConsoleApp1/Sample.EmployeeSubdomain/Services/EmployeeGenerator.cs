﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.Sdk.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Services
{
    public class EmployeeGenerator : IHostedService
    {
        private CancellationTokenSource _tokenSource;
        private readonly ILogger<EmployeeGenerator> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmployeeGenerator(
            ILogger<EmployeeGenerator> logger,
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _tokenSource.Token;
            return Task.Run(async () => 
            {
                try
                {
                    var rnd = new Random();
                    var employees = new List<Tuple<string, string>>();
                    for (var i = 0; i < 1; i++)
                    {
                        employees.Add(new Tuple<string, string>($"yusbel{rnd.Next(0, 100)}", $"yusbel@gmail.com {rnd.Next(0, 100)}"));
                    }
                    await Parallel.ForEachAsync(employees, token, async (employee, stopToken) =>
                    {
                        try
                        {
                            if (stopToken.IsCancellationRequested)
                                stopToken.ThrowIfCancellationRequested();
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var employeeService = scope.ServiceProvider.GetRequiredService<IEmployee>();
                                await employeeService.CreateAndSave(employee.Item1, employee.Item2, token).ConfigureAwait(false);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            e.LogCriticalException(_logger, $"Error, managed thread: {Thread.CurrentThread.ManagedThreadId}");
                        }
                    }).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce) 
                {
                    oce.LogCriticalException(_logger, "Operation canceled exception");
                }
                catch (Exception e)
                {
                    e.LogCriticalException(_logger, "Error, managed thread");
                }
                _logger.LogInformation("Employee generator finished");
            }, token);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stop async was called");
            _tokenSource?.Dispose();
            return Task.CompletedTask;
        }
    }
}