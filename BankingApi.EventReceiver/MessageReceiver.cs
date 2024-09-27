using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using BankingApi.EventReceiver.Models;
using Microsoft.Azure.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BankingApi.EventReceiver
{
    public class MessageReceiver : IServiceBusReceiver
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusReceiver _receiver;
        private readonly string _connectionString = Environment.GetEnvironmentVariable("connectionString");
        private readonly string _queueName = Environment.GetEnvironmentVariable("queueName");
        private string _currentMessageId;
        private readonly ServiceBusAdministrationClient _adminClient;

        public MessageReceiver()
        {
            _client = new ServiceBusClient(_connectionString);
            _receiver = _client.CreateReceiver(_queueName);
            _adminClient = new ServiceBusAdministrationClient(_connectionString);
        }
        public Task Abandon(EventMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task Complete(EventMessage message)
        {
            try
            {
                ServiceBusReceivedMessage messageToComplete = await findMessageAsync(message).ConfigureAwait(false);

                await _receiver.CompleteMessageAsync(messageToComplete);
                               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await _receiver.DisposeAsync();
                await _client.DisposeAsync();
            }
        }

        public Task MoveToDeadLetter(EventMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task<EventMessage?> Peek()
        {
            var receivedMessages = await _receiver.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(10));

            if (receivedMessages.Count == 0)
            {
                return null;
            }

            var receivedMessage = receivedMessages[0];

            _currentMessageId = receivedMessage.MessageId;

            var eventMessage = JsonSerializer.Deserialize<EventMessage>(receivedMessage.Body.ToString());

            return eventMessage;
        }

        public Task ReSchedule(EventMessage message, DateTime nextAvailableTime)
        {
            throw new NotImplementedException();
        }


        public async Task<ServiceBusReceivedMessage> findMessageAsync(EventMessage eventMessage)
        {
            var administrationClient = new ServiceBusAdministrationClient(_connectionString);
            var props = await administrationClient.GetQueueRuntimePropertiesAsync(_queueName);
            var messageCount = props.Value.ActiveMessageCount;
            if (messageCount > 0)
            {
                // get all messages in the queue

                IList<ServiceBusReceivedMessage> messages = (IList<ServiceBusReceivedMessage>)ReceiveLargeBatchMessagesAsync(_receiver, messageCount, 100);

                // Find the message with the specified message ID
                var message = messages.FirstOrDefault(m => m.MessageId == _currentMessageId);
                return message;
            }
            return null;
        }

        public async Task<List<ServiceBusReceivedMessage>> ReceiveLargeBatchMessagesAsync(ServiceBusReceiver receiver, long totalMessagesToReceive, int batchSize)
        {
            var allMessages = new List<ServiceBusReceivedMessage>();
            long remainingMessages = totalMessagesToReceive;

            while (remainingMessages > 0)
            {
                // Determine the size of the next batch
                int currentBatchSize = (int)Math.Min(batchSize, remainingMessages);

                // Receive a batch of messages
                var messages = await receiver.ReceiveMessagesAsync(currentBatchSize, TimeSpan.FromSeconds(30));

                if (messages == null || messages.Count == 0)
                {
                    // No more messages available
                    break;
                }

                allMessages.AddRange(messages);
                remainingMessages -= messages.Count;
            }

            return allMessages;
        }
    }
    
}
