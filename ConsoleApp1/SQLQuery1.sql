select * from ExternalEvents

select count(*) from InComingEvents where WasProcessed = 1 and WasAcknowledge = 1

delete from InComingEvents
delete from ExternalEvents
delete from Logs

select * from Logs