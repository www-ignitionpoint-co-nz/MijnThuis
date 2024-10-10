﻿using MijnThuis.Integrations.Solar;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class HomeBatteryNotificationWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HomeBatteryNotificationWorker> _logger;

    public HomeBatteryNotificationWorker(
        IConfiguration configuration, IServiceProvider serviceProvider,
        ILogger<HomeBatteryNotificationWorker> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sendGridApiKey = _configuration.GetValue<string>("SENDGRID_API_KEY");
        var sendGridSender = _configuration.GetValue<string>("SENDGRID_SENDER");
        var sendGridReceiver = _configuration.GetValue<string>("SENDGRID_RECEIVER");

        DateOnly? notifiedFullBatteryToday = null;
        DateOnly? notifiedLowBatteryToday = null;
        DateOnly? notifiedEmptyBatteryToday = null;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            if (notifiedFullBatteryToday.HasValue && notifiedFullBatteryToday.Value != DateOnly.FromDateTime(DateTime.Today))
            {
                notifiedFullBatteryToday = null;
            }

            if (notifiedLowBatteryToday.HasValue && notifiedLowBatteryToday.Value != DateOnly.FromDateTime(DateTime.Today))
            {
                notifiedLowBatteryToday = null;
            }

            if (notifiedEmptyBatteryToday.HasValue && notifiedEmptyBatteryToday.Value != DateOnly.FromDateTime(DateTime.Today))
            {
                notifiedEmptyBatteryToday = null;
            }

            // Initialize dependencies and variables.
            using var serviceScope = _serviceProvider.CreateScope();
            var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();

            // Get information about solar energy.
            var solarOverview = await solarService.GetOverview();

            if (solarOverview.BatteryLevel == 100 && notifiedFullBatteryToday == null)
            {
                notifiedFullBatteryToday = DateOnly.FromDateTime(DateTime.Today);
                await SendEmail("De thuisbatterij is volledig opgeladen (100%)!",
                    sendGridSender, sendGridReceiver, sendGridApiKey);
            }

            if (solarOverview.BatteryLevel < 20 && notifiedLowBatteryToday == null)
            {
                notifiedLowBatteryToday = DateOnly.FromDateTime(DateTime.Today);
                await SendEmail("De thuisbatterij is bijna leeg (< 20%)!",
                    sendGridSender, sendGridReceiver, sendGridApiKey);
            }

            if (solarOverview.BatteryLevel == 0 && notifiedEmptyBatteryToday == null)
            {
                notifiedEmptyBatteryToday = DateOnly.FromDateTime(DateTime.Today);
                await SendEmail("De thuisbatterij is helemaal leeg (0%)!",
                    sendGridSender, sendGridReceiver, sendGridApiKey);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // Wait for a maximum of 5 minutes before the next iteration.
            var duration = TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }

    private async Task SendEmail(string message, string sendGridSender, string sendGridReceiver, string apiKey)
    {
        var client = new SendGridClient(apiKey);

        var email = MailHelper.CreateSingleEmail(
            new EmailAddress(sendGridSender),
            new EmailAddress(sendGridReceiver),
            "MijnThuis - Thuisbatterij notificatie",
            message, message);

        await client.SendEmailAsync(email);
    }
}