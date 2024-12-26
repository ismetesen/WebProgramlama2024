CREATE TABLE [dbo].[Users] (
    [UserID]           INT            IDENTITY (1, 1) NULL,
    [FullName]         NVARCHAR (MAX) NOT NULL,
    [Email]            NVARCHAR (450) NOT NULL,
    [PasswordHash]     NVARCHAR (MAX) NOT NULL,
    [PhoneNumber]      NVARCHAR (MAX) NOT NULL,
    [RegistrationDate] DATETIME2 (7)  NULL,
    [Role]             NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email]
    ON [dbo].[Users]([Email] ASC);

