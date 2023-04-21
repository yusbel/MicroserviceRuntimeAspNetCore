# Microservice runtime draft


## Concept 
* Microservices are bound to a subdomain transaction
* Each microservice use a persistence object to save entities and events
* A runtime generic host read events and dispatch them into Azure Service Bus Queue
* Service to service communication is done via Azure Service Bus Queue


![alt_text][concept]

[concept]: https://learningruntimestor.blob.core.windows.net/runtimedocumentation/MicroserviceConceptAndDb120by120.png "Microservice concept"



### Azure Service Bus configuration
* Each microservice load at startup the configuration of queue to send messages
* Service bus instance are created and stored in a concurrent collection during startup

### Queue configuration
* Each sender queue have an acknowledge queue if acknowledge for message received and processed is expected by the sender microservice
* Each message have a DecryptScope that is required to decrypt message using the service runtime local crypto endpoint (it would be explained later)
* Each queue is setup in a service endpoint. Receiver microserivce must validate the message intransit data (defined later) like the endpoint match the endpoint from where the message was received. This would provide gurantee that message are send to the queue and received from the queue that was intended too.   

```
"Sender": [
          {
            "ConfigType": "Sender",
            "ConnStr": "Endpoint=sb://leraningyusbel.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SharedAccessKeyValueGoesHere",
            "Identifier": "ServiceBusClientEmployeeMessages",
            "MessageInTransitOptions": [
              {
                "MsgQueueName": "EmployeeAdded",
                "AckQueueName" :  "AckEmployeeAdded",
                "MsgDecryptScope": "EmployeeAdded.Decrypt",
                "MsgQueueEndpoint": "leraningyusbel.servicebus.windows.net"
              },
              {
                "MsgQueueName": "EmployeeUpdated",
                "AckQueueName" : "",
                "MsgDecryptScope": "EmployeeUpdated.Decrypt",
                "MsgQueueEndpoint": "leraningyusbel.servicebus.windows.net"
              },
              {
                "MsgQueueName": "EmployeeDeleted",
                "AckQueueName" :  "",
                "MsgDecryptScope": "EmployeeDeleted.Decrypt",
                "MsgQueueEndpoint": "leraningyusbel.servicebus.windows.net"
              }
            ]
          }
        ]
```
### Azure service bus receiver configuration
* Each microservice load the receiver configuration and add a service bus processor into a concurrent collection

```
"Receiver": [
          {
            "ConfigType": "Receiver",
            "ConnStr": "Endpoint=sb://leraningyusbel.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SharedAccessKeyGoesHere",
            "Identifier": "ServiceBusClientPayRollMessages",
            "MessageInTransitOptions": [
              {
                "MsgQueueName": "PayRollAdded",
                "AckQueueName":  "",
                "MsgDecryptScope": "PayRollAdded.Decrypt",
                "MsgQueueEndpoint": "leraningyusbel.servicebus.windows.net"
              },
              {
                "MsgQueueName": "PayRollUpdated",
                "AckQueueName":  "",
                "MsgDecryptScope": "PayRollUpdated.Decrypt",
                "MsgQueueEndpoint": "leraningyusbel.servicebus.windows.net"
              },
              {
                "MsgQueueName": "PayRollDeleted",
                "AckQueueName": "",
                "MsgDecryptScope": "PayRollDeleted.Decrypt",
                "MsgQueueEndpoint": "leraningyusbel.servicebus.windows.net"
              }
            ]
          }
        ]
```

### Save entity and event
* Save entity and event in a transaction 
```
public async Task<bool> SaveWithEvent(OutgoingEventEntity eventEntity, CancellationToken token)
        {
            _dbContext.Add(eventEntity);
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            if (token.IsCancellationRequested)
                return false;

            await strategy.ExecuteInTransactionAsync(
                operation: async () =>
                    {
                        await _dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false).ConfigureAwait(false);
                    },
                verifySucceeded: async () =>
                    {
                        return await _dbContext.Set<OutgoingEventEntity>().AsNoTracking().AnyAsync(ee => ee.Id == eventEntity.Id).ConfigureAwait(false);
                    }).ConfigureAwait(false);

            _dbContext.ChangeTracker.AcceptAllChanges();
            if (OnSave != null)
            {
                OnSave(this, new ExternalMessageEventArgs()
                {
                    ExternalMessage = eventEntity!.ConvertToExternalMessage()
                });
            }
            return true;
        }
```

# Runtime
A microservice assembly is loaded and invoked by a service host runtime. The service host runtime do:
* Set environment variables 
* Setup service dependecies in Azure like: registering the service in active directory, manage secret, key and policies in azure key vault, load configuration values from Azure App Configuration.
* Verify service dependecies status.

![alt_text][runtime]

[runtime]: https://learningruntimestor.blob.core.windows.net/runtimedocumentation/RuntimeSample.png "Runtime activity diagram"

### Load microservice assembly
* Microservie assembly location are stored on an environment variable or can be passed as a command argument
* Runtime is nothing more that a generic host application
* Runtime excecute a hosted service to setup azure service and verify their configuration status on an schedule

### Sample code that load the microservice assembly and execute the run async
```
private static bool LaunchCoreHostApp(string[] args, 
            ServiceHostBuilderOption service, 
            ServiceRegistration serviceReg,
            CancellationToken runtimeToken) 
        {
            var logger = GetLogger();
            if (!File.Exists(service.Service))
                return false;
            var hostService = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(service.Service, service.HostBuilder);

            if (hostService is ICoreHostBuilder serviceHost)
            {
                var serviceHostBuilder = serviceHost.GetHostBuilder(args);
                try
                {
                    if (serviceReg == null)
                        throw new InvalidOperationException();
                    _ = Task.Run(async () => 
                    {
                        await HostService.Run(serviceHostBuilder, serviceReg, args, runtimeToken)
                                        .ConfigureAwait(false);
                    }, runtimeToken);
                    return true;
                }
                catch (Exception e)
                {
                    e.LogException(logger.LogCritical);
                }
            }
            return false;
        }
```


## Create employee activity diagram
* Once an employee is create a new event EmployeeAdded is raised
* PayRoll microservice is subscribed to EmployeeAdded queue 
* PayRoll create a payroll entry for the new created employee and send an acknowledge message to employee service

### Activity diagram

![alt_text][createemployee]

[createemployee]: https://learningruntimestor.blob.core.windows.net/runtimedocumentation/CreateEmployeeActivityDiagram.png "Employee Added"
