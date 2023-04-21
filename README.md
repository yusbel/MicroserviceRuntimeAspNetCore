# Microservice runtime draft

## Overview
Most microservices implementations relay on REST endpoints for interprocess communications. I'm working through a concept in which REST endpoint are not used for interprocess communication, this is a most to do when propagating write operations across microservices. A write operation that span multiple microservices is a distributed transaction that it should not be implented using REST endpoint. Lets said you have an orchestration layer or microservice that orchestrate operations across microservices. Let's say, that to create an employee the architecture use a orchestration microservice that invoke the OnBoarding microservice, upon valid respond it invoke the PayRoll microservice. In this scenerio, we have a distributed transaction, there are solutions to deal with failure, like using compensation patterns to revert changes done. The architecture gets complicated, it reseemble the n-tier architecture rather than microservice architecture that focus on domain functionalities. Microservices usually are not restricted like the n-tier architecture in which a tier layer can only talk to a certain tier(s)(strict or relax), etc, microservice are cohesive and solve a domain problem, they emit events and they can be invoked by any other microservices. Microservies can be protected by creating network controls to limit the access to their interfaces.

Read operations, when a microservice need data from another service it's typical to use a REST endpoint to retireve the data to compute the incoming request. The alternative to this approach is to collocate data on each microservice given the cost of collocating data is acceptable. Why?, collocating data on each microservice increase their autonomy, availability and performace. This can be done in those service where the availability and performance is core or a must in your microservices implementations rather than a hammer for every read operation. There is another alternative, create a group of service that are read-only services, these services are subscriber to the domain emitted events to create documents that match the read operation required by the client applications. These group of services will reduce the amount of data duplication, one cons is that they have an old snaphot of the data. The data they provide is behind according to the latency between emitted events and their computation. 


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
* Each sender queue have an acknowledge queue if acknowledge for message received and processed is expected by the sender microservice. The ackqueue is part of configuration to validate that the reply to in a message when used match the configuration. This control enable to secure that message are routed through queues as intended during the architecture cycle. 
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
