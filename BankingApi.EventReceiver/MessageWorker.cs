using Azure.Messaging.ServiceBus;
using BankingApi.EventReceiver.Interfaces;
using BankingApi.EventReceiver.Models;
using Microsoft.Azure.Amqp.Framing;
using System.Text.Json;

namespace BankingApi.EventReceiver
{
    public class MessageWorker
    {
        private readonly IServiceBusReceiver _serviceBusReceiver;
        private readonly IBankAccountService _bankAccountService;
        public MessageWorker(IServiceBusReceiver serviceBusReceiver, IBankAccountService bankAccountService, string connectionString, string queueName)
        {
            _serviceBusReceiver = serviceBusReceiver;
            _bankAccountService = bankAccountService;
        }

        public async Task<Task> Start()
        {
            // Implement logic to listen to messages here
            int[] retryDelays = { 5, 25, 125 };
            int retryCount = 0;
            EventMessage eventMessage = null;
            try
            {
                eventMessage = await _serviceBusReceiver.Peek();
                if (eventMessage != null)
                {
                    var CreditDebitMessage = JsonSerializer.Deserialize<CreditDebit>(eventMessage?.MessageBody);

                    if (CreditDebitMessage?.MessageType == "Debit")
                    {
                        _bankAccountService.DeductAmount(CreditDebitMessage.BankAccountId, CreditDebitMessage.Amount);
                        _serviceBusReceiver.Complete(eventMessage);
                    }
                    else if (CreditDebitMessage?.MessageType == "Credit")
                    {
                        _bankAccountService.AddAmount(CreditDebitMessage.BankAccountId, CreditDebitMessage.Amount);
                        _serviceBusReceiver.Complete(eventMessage);
                    }
                    else
                    {
                        _serviceBusReceiver.MoveToDeadLetter(eventMessage);
                    }
                }
            }
            catch (ServiceBusException ex)
            {
                if (ex.IsTransient)
                {
                    if (retryCount < retryDelays.Length)
                    {
                        Console.WriteLine($"Transient error occurred. Retrying in {retryDelays[retryCount]} seconds...");
                        await Task.Delay(retryDelays[retryCount] * 1000); // Convert seconds to milliseconds
                        retryCount++;
                    }
                    else
                    {
                        await _serviceBusReceiver.MoveToDeadLetter(eventMessage);
                    }
                }
                else
                {
                    await _serviceBusReceiver.MoveToDeadLetter(eventMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}
