IF OBJECT_ID(N'dbo.AppNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppNotifications
    (
        NotificationId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_AppNotifications PRIMARY KEY,
        UserId BIGINT NOT NULL,
        FromUserId BIGINT NULL,
        NotificationType NVARCHAR(30) NOT NULL,
        Title NVARCHAR(120) NOT NULL,
        Body NVARCHAR(500) NOT NULL,
        RelatedSupplyId BIGINT NULL,
        RelatedOrderId BIGINT NULL,
        RelatedThreadId BIGINT NULL,
        RelatedChatRequestId BIGINT NULL,
        IsRead BIT NOT NULL
            CONSTRAINT DF_AppNotifications_IsRead DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_AppNotifications_CreatedAt DEFAULT (SYSDATETIME()),
        ReadAt DATETIME2(0) NULL,
        CONSTRAINT FK_AppNotifications_User
            FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_AppNotifications_FromUser
            FOREIGN KEY (FromUserId) REFERENCES dbo.Users(UserId)
    );
END;
GO

IF COL_LENGTH(N'dbo.AppNotifications', N'RelatedChatRequestId') IS NULL
    ALTER TABLE dbo.AppNotifications ADD RelatedChatRequestId BIGINT NULL;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.AppNotifications')
      AND name = N'IX_AppNotifications_UserReadCreated'
)
    CREATE INDEX IX_AppNotifications_UserReadCreated
        ON dbo.AppNotifications(UserId, IsRead, CreatedAt DESC);
GO

CREATE OR ALTER PROCEDURE dbo.ManageAppNotifications
    @Action NVARCHAR(20),
    @ActorUserId BIGINT,
    @ActorRole NVARCHAR(20),
    @NotificationId BIGINT = NULL,
    @ToUserId BIGINT = NULL,
    @ToRole NVARCHAR(20) = NULL,
    @NotificationType NVARCHAR(30) = NULL,
    @Title NVARCHAR(120) = NULL,
    @Body NVARCHAR(500) = NULL,
    @RelatedSupplyId BIGINT = NULL,
    @RelatedOrderId BIGINT = NULL,
    @RelatedThreadId BIGINT = NULL,
    @RelatedChatRequestId BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Action = UPPER(LTRIM(RTRIM(COALESCE(@Action, N''))));
    SET @ActorRole = UPPER(LTRIM(RTRIM(COALESCE(@ActorRole, N''))));
    SET @ToRole = UPPER(LTRIM(RTRIM(COALESCE(@ToRole, N''))));

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 50300, 'A valid actor user ID is required.', 1;

    IF @ActorRole NOT IN (N'OWNER', N'SUPPLIER', N'CONSUMER')
        THROW 50301, 'Role must be Owner, Supplier, or Consumer.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Users
        WHERE UserId = @ActorUserId
          AND IsActive = 1
          AND UPPER(LTRIM(RTRIM(UserRole))) = @ActorRole
    )
        THROW 50302, 'Active user not found or role mismatch.', 1;

    IF @Action = N'GETMINE'
    BEGIN
        SELECT
            n.NotificationId,
            n.UserId AS ToUserId,
            UPPER(LTRIM(RTRIM(recipient.UserRole))) AS ToRole,
            COALESCE(sender.FullName, N'FreshMilk') AS FromName,
            UPPER(LTRIM(RTRIM(COALESCE(sender.UserRole, N'SYSTEM')))) AS FromRole,
            n.NotificationType,
            n.Title,
            n.Body,
            n.RelatedSupplyId,
            n.RelatedOrderId,
            n.RelatedThreadId,
            n.RelatedChatRequestId,
            n.IsRead,
            n.CreatedAt,
            CONVERT(INT, unread.UnreadCount) AS UnreadCount
        FROM dbo.AppNotifications AS n
        INNER JOIN dbo.Users AS recipient ON recipient.UserId = n.UserId
        LEFT JOIN dbo.Users AS sender ON sender.UserId = n.FromUserId
        CROSS APPLY
        (
            SELECT COUNT_BIG(1) AS UnreadCount
            FROM dbo.AppNotifications AS pending
            WHERE pending.UserId = @ActorUserId
              AND pending.IsRead = 0
        ) AS unread
        WHERE n.UserId = @ActorUserId
        ORDER BY n.CreatedAt DESC, n.NotificationId DESC;
        RETURN;
    END;

    IF @Action = N'MARKREAD'
    BEGIN
        IF @NotificationId IS NULL OR @NotificationId <= 0
            THROW 50303, 'A valid notification ID is required.', 1;

        UPDATE dbo.AppNotifications
        SET IsRead = 1, ReadAt = COALESCE(ReadAt, SYSDATETIME())
        WHERE NotificationId = @NotificationId
          AND UserId = @ActorUserId;

        SELECT @@ROWCOUNT AS AffectedCount;
        RETURN;
    END;

    IF @Action = N'CREATE'
    BEGIN
        IF @ActorRole <> N'OWNER' AND @ToRole = N'OWNER' AND @ToUserId IS NULL
        BEGIN
            SELECT TOP (1) @ToUserId = UserId
            FROM dbo.Users
            WHERE IsActive = 1
              AND UPPER(LTRIM(RTRIM(UserRole))) = N'OWNER'
            ORDER BY UserId;
        END;

        IF @ToUserId IS NULL OR @ToUserId <= 0
            THROW 50304, 'A valid notification recipient is required.', 1;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Users
            WHERE UserId = @ToUserId
              AND IsActive = 1
              AND (@ToRole = N'' OR UPPER(LTRIM(RTRIM(UserRole))) = @ToRole)
        )
            THROW 50305, 'Active notification recipient not found or role mismatch.', 1;

        IF NULLIF(LTRIM(RTRIM(@NotificationType)), N'') IS NULL
           OR NULLIF(LTRIM(RTRIM(@Title)), N'') IS NULL
           OR NULLIF(LTRIM(RTRIM(@Body)), N'') IS NULL
            THROW 50306, 'Notification type, title, and body are required.', 1;

        DECLARE @NewNotificationId BIGINT;
        INSERT INTO dbo.AppNotifications
        (
            UserId, FromUserId, NotificationType, Title, Body,
            RelatedSupplyId, RelatedOrderId, RelatedThreadId, RelatedChatRequestId
        )
        VALUES
        (
            @ToUserId, @ActorUserId, UPPER(LTRIM(RTRIM(@NotificationType))),
            LTRIM(RTRIM(@Title)), LTRIM(RTRIM(@Body)),
            @RelatedSupplyId, @RelatedOrderId, @RelatedThreadId, @RelatedChatRequestId
        );
        SET @NewNotificationId = CONVERT(BIGINT, SCOPE_IDENTITY());

        SELECT
            n.NotificationId,
            n.UserId AS ToUserId,
            UPPER(LTRIM(RTRIM(recipient.UserRole))) AS ToRole,
            COALESCE(sender.FullName, N'FreshMilk') AS FromName,
            UPPER(LTRIM(RTRIM(COALESCE(sender.UserRole, N'SYSTEM')))) AS FromRole,
            n.NotificationType,
            n.Title,
            n.Body,
            n.RelatedSupplyId,
            n.RelatedOrderId,
            n.RelatedThreadId,
            n.RelatedChatRequestId,
            n.IsRead,
            n.CreatedAt,
            CONVERT(INT, 1) AS UnreadCount
        FROM dbo.AppNotifications AS n
        INNER JOIN dbo.Users AS recipient ON recipient.UserId = n.UserId
        LEFT JOIN dbo.Users AS sender ON sender.UserId = n.FromUserId
        WHERE n.NotificationId = @NewNotificationId;
        RETURN;
    END;

    THROW 50307, 'Invalid notification action.', 1;
END;
GO