USE [EmployeeDb]
GO

/****** Object: Table [dbo].[InComingEvents] Script Date: 2023-03-19 7:21:50 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

DROP TABLE [dbo].[InComingEvents];


GO
CREATE TABLE [dbo].[InComingEvents] (
    [Id]                      NVARCHAR (50)  NOT NULL,
    [Scheme]                  NVARCHAR (50)  NULL,
    [Type]                    NVARCHAR (50)  NULL,
    [Version]                 NVARCHAR (50)  NULL,
    [Body]                    NVARCHAR (MAX) NULL,
    [CreationTime]            BIGINT         NULL,
    [IsDeleted]               BIT            NULL,
    [WasAcknowledge]          BIT            NULL,
    [MessageKey]              NVARCHAR (50)  NULL,
    [WasProcessed]            BIT            NULL,
    [ServiceIdentifier]       NVARCHAR (50)  NOT NULL,
    [CertificateLocation]     NVARCHAR (450) NULL,
    [CertificateKey]          NVARCHAR (250) NULL,
    [MsgQueueEndpoint]        NVARCHAR (450) NULL,
    [MsgQueueName]            NVARCHAR (250) NULL,
    [MsgDecryptScope]         NVARCHAR (250) NULL,
    [WellknownEndpoint]       NVARCHAR (450) NULL,
    [DecryptEndpoint]         NVARCHAR (450) NULL,
    [AcknowledgementEndpoint] NVARCHAR (250) NULL
);


