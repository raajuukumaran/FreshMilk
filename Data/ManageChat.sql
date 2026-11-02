IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages
    (
        MessageId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ChatMessages PRIMARY KEY,
        ThreadId BIGINT NOT NULL,
        SenderUserId BIGINT NOT NULL,
        RecipientUserId BIGINT NOT NULL,
        MessageText NVARCHAR(2000) NOT NULL,
        IsRead BIT NOT NULL
            CONSTRAINT DF_ChatMessages_IsRead DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT (SYSDATETIME()),
        ReadAt DATETIME2(0) NULL,
        CONSTRAINT FK_ChatMessages_Sender
            FOREIGN KEY (SenderUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ChatMessages_Recipient
            FOREIGN KEY (RecipientUserId) REFERENCES dbo.Users(UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.ChatThreads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatThreads
    (
        ThreadId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ChatThreads PRIMARY KEY,
        OwnerUserId BIGINT NOT NULL,
        RequesterUserId BIGINT NULL,
        RequesterRole NVARCHAR(20) NULL,
        ParticipantUserId BIGINT NOT NULL,
        ParticipantRole NVARCHAR(20) NOT NULL,
        ThreadKind NVARCHAR(20) NOT NULL
            CONSTRAINT DF_ChatThreads_ThreadKind DEFAULT (N'OWNER'),
        IsOwnerApproved BIT NOT NULL
            CONSTRAINT DF_ChatThreads_IsOwnerApproved DEFAULT (1),
        RelatedSupplyId BIGINT NULL,
        AccessStartsAt DATETIME2(0) NULL,
        AccessEndsAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_ChatThreads_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_ChatThreads_UpdatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_ChatThreads_Owner
            FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ChatThreads_Requester
            FOREIGN KEY (RequesterUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ChatThreads_Participant
            FOREIGN KEY (ParticipantUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_ChatThreads_ParticipantRole
            CHECK (ParticipantRole IN (N'OWNER', N'SUPPLIER', N'CONSUMER')),
        CONSTRAINT CK_ChatThreads_ThreadKind
            CHECK (ThreadKind IN (N'OWNER', N'PEER', N'SUPPLY', N'DIRECT'))
    );
END;
GO

IF COL_LENGTH(N'dbo.ChatThreads', N'RequesterUserId') IS NULL
    ALTER TABLE dbo.ChatThreads ADD RequesterUserId BIGINT NULL;
IF COL_LENGTH(N'dbo.ChatThreads', N'RequesterRole') IS NULL
    ALTER TABLE dbo.ChatThreads ADD RequesterRole NVARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.ChatThreads', N'ThreadKind') IS NULL
    ALTER TABLE dbo.ChatThreads ADD ThreadKind NVARCHAR(20) NOT NULL
        CONSTRAINT DF_ChatThreads_ThreadKind DEFAULT (N'OWNER');
IF COL_LENGTH(N'dbo.ChatThreads', N'IsOwnerApproved') IS NULL
    ALTER TABLE dbo.ChatThreads ADD IsOwnerApproved BIT NOT NULL
        CONSTRAINT DF_ChatThreads_IsOwnerApproved DEFAULT (1);
IF COL_LENGTH(N'dbo.ChatThreads', N'RelatedSupplyId') IS NULL
    ALTER TABLE dbo.ChatThreads ADD RelatedSupplyId BIGINT NULL;
IF COL_LENGTH(N'dbo.ChatThreads', N'AccessStartsAt') IS NULL
    ALTER TABLE dbo.ChatThreads ADD AccessStartsAt DATETIME2(0) NULL;
IF COL_LENGTH(N'dbo.ChatThreads', N'AccessEndsAt') IS NULL
    ALTER TABLE dbo.ChatThreads ADD AccessEndsAt DATETIME2(0) NULL;
GO

UPDATE dbo.ChatThreads
SET RequesterUserId = OwnerUserId,
    RequesterRole = N'OWNER'
WHERE RequesterUserId IS NULL OR RequesterRole IS NULL;
GO

ALTER TABLE dbo.ChatThreads ALTER COLUMN RequesterUserId BIGINT NOT NULL;
ALTER TABLE dbo.ChatThreads ALTER COLUMN RequesterRole NVARCHAR(20) NOT NULL;

IF EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatThreads')
      AND name = N'CK_ChatThreads_ThreadKind'
      AND definition NOT LIKE N'%PEER%'
)
    ALTER TABLE dbo.ChatThreads DROP CONSTRAINT CK_ChatThreads_ThreadKind;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatThreads')
      AND name = N'CK_ChatThreads_ThreadKind'
)
    ALTER TABLE dbo.ChatThreads WITH CHECK
        ADD CONSTRAINT CK_ChatThreads_ThreadKind
            CHECK (ThreadKind IN (N'OWNER', N'PEER', N'SUPPLY', N'DIRECT'));
GO

IF EXISTS
(
    SELECT 1 FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatThreads')
      AND name = N'UQ_ChatThreads_OwnerParticipant'
)
    ALTER TABLE dbo.ChatThreads DROP CONSTRAINT UQ_ChatThreads_OwnerParticipant;
GO

IF EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatThreads')
      AND name = N'CK_ChatThreads_ParticipantRole'
)
    ALTER TABLE dbo.ChatThreads DROP CONSTRAINT CK_ChatThreads_ParticipantRole;
ALTER TABLE dbo.ChatThreads WITH CHECK
    ADD CONSTRAINT CK_ChatThreads_ParticipantRole
        CHECK (ParticipantRole IN (N'OWNER', N'SUPPLIER', N'CONSUMER'));
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ChatThreads_Requester'
      AND parent_object_id = OBJECT_ID(N'dbo.ChatThreads')
)
    ALTER TABLE dbo.ChatThreads
        ADD CONSTRAINT FK_ChatThreads_Requester
            FOREIGN KEY (RequesterUserId) REFERENCES dbo.Users(UserId);
GO

IF OBJECT_ID(N'dbo.ChatAccessRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatAccessRequests
    (
        RequestId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ChatAccessRequests PRIMARY KEY,
        RequesterUserId BIGINT NOT NULL,
        RequesterRole NVARCHAR(20) NOT NULL,
        TargetUserId BIGINT NOT NULL,
        TargetRole NVARCHAR(20) NOT NULL,
        RelatedSupplyId BIGINT NULL,
        RequestNote NVARCHAR(500) NULL,
        RequestStatus NVARCHAR(20) NOT NULL
            CONSTRAINT DF_ChatAccessRequests_Status DEFAULT (N'PENDING_TARGET'),
        ThreadId BIGINT NULL,
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_ChatAccessRequests_CreatedAt DEFAULT (SYSDATETIME()),
        ReviewedByOwnerId BIGINT NULL,
        ReviewedAt DATETIME2(0) NULL,
        CONSTRAINT FK_ChatAccessRequests_Requester
            FOREIGN KEY (RequesterUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ChatAccessRequests_Target
            FOREIGN KEY (TargetUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ChatAccessRequests_Owner
            FOREIGN KEY (ReviewedByOwnerId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ChatAccessRequests_Thread
            FOREIGN KEY (ThreadId) REFERENCES dbo.ChatThreads(ThreadId),
        CONSTRAINT CK_ChatAccessRequests_Status
            CHECK (RequestStatus IN
                (N'PENDING_TARGET', N'PENDING_OWNER', N'APPROVED', N'REJECTED'))
    );
END;
GO

IF EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatAccessRequests')
      AND name = N'CK_ChatAccessRequests_Status'
)
    ALTER TABLE dbo.ChatAccessRequests
        DROP CONSTRAINT CK_ChatAccessRequests_Status;
GO

UPDATE dbo.ChatAccessRequests
SET RequestStatus = N'PENDING_OWNER'
WHERE RequestStatus = N'PENDING';
GO

ALTER TABLE dbo.ChatAccessRequests WITH CHECK
    ADD CONSTRAINT CK_ChatAccessRequests_Status
        CHECK (RequestStatus IN
            (N'PENDING_TARGET', N'PENDING_OWNER', N'APPROVED', N'REJECTED'));
GO

IF EXISTS
(
    SELECT 1 FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ChatAccessRequests')
      AND name = N'DF_ChatAccessRequests_Status'
)
    ALTER TABLE dbo.ChatAccessRequests
        DROP CONSTRAINT DF_ChatAccessRequests_Status;
GO

ALTER TABLE dbo.ChatAccessRequests
    ADD CONSTRAINT DF_ChatAccessRequests_Status DEFAULT (N'PENDING_TARGET')
        FOR RequestStatus;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ChatAccessRequests')
      AND name = N'IX_ChatAccessRequests_StatusCreated'
)
    CREATE INDEX IX_ChatAccessRequests_StatusCreated
        ON dbo.ChatAccessRequests(RequestStatus, CreatedAt DESC);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ChatMessages_Thread'
      AND parent_object_id = OBJECT_ID(N'dbo.ChatMessages')
)
    ALTER TABLE dbo.ChatMessages
        ADD CONSTRAINT FK_ChatMessages_Thread
            FOREIGN KEY (ThreadId) REFERENCES dbo.ChatThreads(ThreadId);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ChatMessages')
      AND name = N'IX_ChatMessages_ThreadCreated'
)
    CREATE INDEX IX_ChatMessages_ThreadCreated
        ON dbo.ChatMessages(ThreadId, CreatedAt, MessageId);
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ChatMessages')
      AND name = N'IX_ChatMessages_RecipientRead'
)
    CREATE INDEX IX_ChatMessages_RecipientRead
        ON dbo.ChatMessages(RecipientUserId, IsRead, ThreadId);
GO

CREATE OR ALTER PROCEDURE dbo.ManageChat
    @Action NVARCHAR(20),
    @ActorUserId BIGINT,
    @ActorRole NVARCHAR(20),
    @ThreadId BIGINT = NULL,
    @ParticipantUserId BIGINT = NULL,
    @ParticipantRole NVARCHAR(20) = NULL,
    @MessageText NVARCHAR(2000) = NULL,
    @RelatedSupplyId BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Action = UPPER(LTRIM(RTRIM(@Action)));
    SET @ActorRole = UPPER(LTRIM(RTRIM(@ActorRole)));
    SET @ParticipantRole = UPPER(LTRIM(RTRIM(COALESCE(@ParticipantRole, N''))));

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 50200, 'A valid actor user ID is required.', 1;

    IF @ActorRole NOT IN (N'OWNER', N'SUPPLIER', N'CONSUMER')
        THROW 50201, 'Role must be Owner, Supplier, or Consumer.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Users
        WHERE UserId = @ActorUserId
          AND IsActive = 1
          AND UPPER(LTRIM(RTRIM(UserRole))) = @ActorRole
    )
        THROW 50202, 'Active user not found or role mismatch.', 1;

    IF @Action = N'GETTHREADS'
    BEGIN
        SELECT
            t.ThreadId,
            CASE
                WHEN @ActorUserId = t.RequesterUserId THEN t.ParticipantUserId
                WHEN @ActorUserId = t.ParticipantUserId THEN t.RequesterUserId
                ELSE t.ParticipantUserId
            END AS ParticipantUserId,
            CASE
                WHEN @ActorRole = N'OWNER' AND t.ThreadKind = N'PEER'
                    THEN CONCAT(requester.FullName, N' ↔ ', participant.FullName)
                WHEN @ActorUserId = t.RequesterUserId
                    THEN participant.FullName
                ELSE requester.FullName
            END AS ParticipantName,
            CASE
                WHEN @ActorRole = N'OWNER' AND t.ThreadKind = N'PEER'
                    THEN N'PEER'
                WHEN @ActorUserId = t.RequesterUserId
                    THEN t.ParticipantRole
                ELSE t.RequesterRole
            END AS ParticipantRole,
            t.ThreadKind,
            t.RelatedSupplyId,
            t.AccessStartsAt,
            t.AccessEndsAt,
            CONVERT(BIT, CASE
                WHEN @ActorUserId IN (t.RequesterUserId, t.ParticipantUserId)
                 AND
                 (
                     t.ThreadKind = N'OWNER'
                     OR
                     (
                         (t.AccessStartsAt IS NULL OR t.AccessStartsAt <= SYSDATETIME())
                         AND t.AccessEndsAt > SYSDATETIME()
                     )
                 )
                    THEN 1 ELSE 0 END) AS CanSend,
            lastMessage.MessageText AS LastMessage,
            lastMessage.CreatedAt AS LastMessageAt,
            CONVERT(INT, unread.UnreadCount) AS UnreadCount
        FROM dbo.ChatThreads AS t
        INNER JOIN dbo.Users AS requester ON requester.UserId = t.RequesterUserId
        INNER JOIN dbo.Users AS participant ON participant.UserId = t.ParticipantUserId
        OUTER APPLY
        (
            SELECT TOP (1) m.MessageText, m.CreatedAt
            FROM dbo.ChatMessages AS m
            WHERE m.ThreadId = t.ThreadId
            ORDER BY m.CreatedAt DESC, m.MessageId DESC
        ) AS lastMessage
        OUTER APPLY
        (
            SELECT COUNT_BIG(1) AS UnreadCount
            FROM dbo.ChatMessages AS m
            WHERE m.ThreadId = t.ThreadId
              AND m.RecipientUserId = @ActorUserId
              AND m.IsRead = 0
        ) AS unread
            WHERE t.IsOwnerApproved = 1
              AND
              (
                  t.RequesterUserId = @ActorUserId
                  OR t.ParticipantUserId = @ActorUserId
                  OR (@ActorRole = N'OWNER' AND t.OwnerUserId = @ActorUserId
                  AND t.ThreadKind = N'PEER')
              )
        ORDER BY COALESCE(lastMessage.CreatedAt, t.UpdatedAt) DESC, t.ThreadId DESC;
        RETURN;
    END;

    IF @Action = N'GETORCREATE'
    BEGIN
        IF @ParticipantUserId IS NULL OR @ParticipantUserId <= 0
            THROW 50204, 'A valid chat participant is required.', 1;

        IF @ParticipantRole NOT IN (N'OWNER', N'SUPPLIER', N'CONSUMER')
            THROW 50205, 'Participant role must be Owner, Supplier, or Consumer.', 1;

        IF (@ParticipantRole = N'OWNER' AND @ActorRole = N'OWNER')
           OR (@ParticipantRole <> N'OWNER' AND @ActorRole <> N'OWNER')
            THROW 50212, 'Owners may start participant chats directly; users may start direct chats with an owner.', 1;

        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Users
            WHERE UserId = @ParticipantUserId
              AND IsActive = 1
                            AND UPPER(LTRIM(RTRIM(UserRole))) = @ParticipantRole
        )
            THROW 50206, 'Active chat participant not found or role mismatch.', 1;

                DECLARE @ThreadOwnerId BIGINT =
                        CASE WHEN @ActorRole = N'OWNER' THEN @ActorUserId ELSE @ParticipantUserId END;
        DECLARE @CreatedThreadId BIGINT;
        BEGIN TRANSACTION;
                        SELECT @CreatedThreadId = ThreadId
            FROM dbo.ChatThreads WITH (UPDLOCK, HOLDLOCK)
                        WHERE OwnerUserId = @ThreadOwnerId
                            AND RequesterUserId = @ActorUserId
                            AND ParticipantUserId = @ParticipantUserId
                            AND ThreadKind = N'OWNER'
                            AND ISNULL(RelatedSupplyId, 0) = ISNULL(@RelatedSupplyId, 0);

            IF @CreatedThreadId IS NULL
            BEGIN
                INSERT INTO dbo.ChatThreads
                    (OwnerUserId, RequesterUserId, RequesterRole,
                     ParticipantUserId, ParticipantRole, ThreadKind,
                     IsOwnerApproved, RelatedSupplyId)
                VALUES
                    (@ThreadOwnerId, @ActorUserId, @ActorRole,
                     @ParticipantUserId, @ParticipantRole, N'OWNER',
                     1, @RelatedSupplyId);
                SET @CreatedThreadId = CONVERT(BIGINT, SCOPE_IDENTITY());
            END;
        COMMIT TRANSACTION;

        SELECT
            t.ThreadId,
            t.ParticipantUserId,
            p.FullName AS ParticipantName,
            t.ParticipantRole,
            t.ThreadKind,
            t.RelatedSupplyId,
            t.AccessEndsAt,
            CONVERT(BIT, 1) AS CanSend,
            lastMessage.MessageText AS LastMessage,
            lastMessage.CreatedAt AS LastMessageAt,
            CONVERT(INT, unread.UnreadCount) AS UnreadCount
        FROM dbo.ChatThreads AS t
        INNER JOIN dbo.Users AS p ON p.UserId = t.ParticipantUserId
        OUTER APPLY
        (
            SELECT TOP (1) m.MessageText, m.CreatedAt
            FROM dbo.ChatMessages AS m
            WHERE m.ThreadId = t.ThreadId
            ORDER BY m.CreatedAt DESC, m.MessageId DESC
        ) AS lastMessage
        OUTER APPLY
        (
            SELECT COUNT_BIG(1) AS UnreadCount
            FROM dbo.ChatMessages AS m
            WHERE m.ThreadId = t.ThreadId
              AND m.RecipientUserId = @ActorUserId
              AND m.IsRead = 0
        ) AS unread
        WHERE t.ThreadId = @CreatedThreadId;
        RETURN;
    END;

    IF @Action IN (N'GETMESSAGES', N'MARKREAD', N'SEND')
    BEGIN
        IF @ThreadId IS NULL OR @ThreadId <= 0
            THROW 50207, 'A valid chat thread ID is required.', 1;

                DECLARE @OwnerUserId BIGINT;
                DECLARE @RequesterUserId BIGINT;
                DECLARE @ParticipantId BIGINT;
                DECLARE @ThreadKind NVARCHAR(20);
                DECLARE @AccessStartsAt DATETIME2(0);
                DECLARE @AccessEndsAt DATETIME2(0);
        SELECT
            @OwnerUserId = OwnerUserId,
                        @RequesterUserId = RequesterUserId,
                        @ParticipantId = ParticipantUserId,
                        @ThreadKind = ThreadKind,
                        @AccessStartsAt = AccessStartsAt,
                        @AccessEndsAt = AccessEndsAt
        FROM dbo.ChatThreads
        WHERE ThreadId = @ThreadId
                    AND IsOwnerApproved = 1
                    AND
                    (
                            RequesterUserId = @ActorUserId
                            OR ParticipantUserId = @ActorUserId
                            OR (@ActorRole = N'OWNER' AND OwnerUserId = @ActorUserId
                                    AND ThreadKind = N'PEER')
                    );

        IF @OwnerUserId IS NULL
            THROW 50208, 'Chat thread not found for this user.', 1;

        IF @Action = N'GETMESSAGES'
        BEGIN
            UPDATE dbo.ChatMessages
            SET IsRead = 1, ReadAt = SYSDATETIME()
            WHERE ThreadId = @ThreadId
              AND RecipientUserId = @ActorUserId
              AND IsRead = 0;

            SELECT
                m.MessageId,
                m.ThreadId,
                m.SenderUserId,
                sender.FullName AS SenderName,
                m.RecipientUserId,
                t.OwnerUserId,
                t.ThreadKind,
                t.AccessEndsAt,
                m.MessageText,
                m.IsRead,
                m.CreatedAt,
                m.ReadAt
            FROM dbo.ChatMessages AS m
            INNER JOIN dbo.Users AS sender ON sender.UserId = m.SenderUserId
            INNER JOIN dbo.ChatThreads AS t ON t.ThreadId = m.ThreadId
            WHERE m.ThreadId = @ThreadId
            ORDER BY m.CreatedAt, m.MessageId;
            RETURN;
        END;

        IF @Action = N'MARKREAD'
        BEGIN
            UPDATE dbo.ChatMessages
            SET IsRead = 1, ReadAt = SYSDATETIME()
            WHERE ThreadId = @ThreadId
              AND RecipientUserId = @ActorUserId
              AND IsRead = 0;
            SELECT @@ROWCOUNT AS AffectedCount;
            RETURN;
        END;

        SET @MessageText = LTRIM(RTRIM(COALESCE(@MessageText, N'')));
        IF LEN(@MessageText) = 0
            THROW 50209, 'Message text is required.', 1;

        IF @ActorUserId NOT IN (@RequesterUserId, @ParticipantId)
            THROW 50211, 'Owners can review peer conversations but cannot send as a participant.', 1;

        IF @ThreadKind = N'PEER'
           AND
           (
               (@AccessStartsAt IS NOT NULL AND @AccessStartsAt > SYSDATETIME())
               OR @AccessEndsAt IS NULL
               OR @AccessEndsAt <= SYSDATETIME()
           )
            THROW 50233, 'The owner-approved chat window is not currently active.', 1;

        IF @ActorUserId = @RequesterUserId
            SET @ParticipantUserId = @ParticipantId;
        ELSE
            SET @ParticipantUserId = @RequesterUserId;

        INSERT INTO dbo.ChatMessages
            (ThreadId, SenderUserId, RecipientUserId, MessageText)
        VALUES
            (@ThreadId, @ActorUserId, @ParticipantUserId, @MessageText);

        UPDATE dbo.ChatThreads
        SET UpdatedAt = SYSDATETIME()
        WHERE ThreadId = @ThreadId;

        SELECT
            m.MessageId,
            m.ThreadId,
            m.SenderUserId,
            sender.FullName AS SenderName,
            m.RecipientUserId,
            t.OwnerUserId,
            t.ThreadKind,
            t.AccessEndsAt,
            m.MessageText,
            m.IsRead,
            m.CreatedAt,
            m.ReadAt
        FROM dbo.ChatMessages AS m
        INNER JOIN dbo.Users AS sender ON sender.UserId = m.SenderUserId
        INNER JOIN dbo.ChatThreads AS t ON t.ThreadId = m.ThreadId
        WHERE m.MessageId = CONVERT(BIGINT, SCOPE_IDENTITY());
        RETURN;
    END;

    THROW 50210, 'Invalid chat action.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ManageChatRequests
    @Action NVARCHAR(20),
    @ActorUserId BIGINT,
    @ActorRole NVARCHAR(20),
    @RequestId BIGINT = NULL,
    @TargetUserId BIGINT = NULL,
    @TargetRole NVARCHAR(20) = NULL,
    @RelatedSupplyId BIGINT = NULL,
    @RequestNote NVARCHAR(500) = NULL,
    @AccessStartDate DATE = NULL,
    @AccessEndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Action = UPPER(LTRIM(RTRIM(COALESCE(@Action, N''))));
    SET @ActorRole = UPPER(LTRIM(RTRIM(COALESCE(@ActorRole, N''))));
    SET @TargetRole = UPPER(LTRIM(RTRIM(COALESCE(@TargetRole, N''))));

    IF @ActorUserId IS NULL OR @ActorUserId <= 0
        THROW 50220, 'A valid actor user ID is required.', 1;

    IF @ActorRole NOT IN (N'OWNER', N'SUPPLIER', N'CONSUMER')
        THROW 50221, 'Role must be Owner, Supplier, or Consumer.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Users
        WHERE UserId = @ActorUserId
          AND IsActive = 1
          AND UPPER(LTRIM(RTRIM(UserRole))) = @ActorRole
    )
        THROW 50222, 'Active user not found or role mismatch.', 1;

    IF @Action = N'GETMINE'
    BEGIN
        SELECT
            r.RequestId,
            r.RequesterUserId,
            requester.FullName AS RequesterName,
            r.RequesterRole,
            r.TargetUserId,
            target.FullName AS TargetName,
            r.TargetRole,
            r.RelatedSupplyId,
            thread.AccessStartsAt,
            thread.AccessEndsAt,
            r.RequestNote,
            r.RequestStatus,
            r.ThreadId,
            r.CreatedAt,
            r.ReviewedByOwnerId,
            r.ReviewedAt
        FROM dbo.ChatAccessRequests AS r
        INNER JOIN dbo.Users AS requester ON requester.UserId = r.RequesterUserId
        INNER JOIN dbo.Users AS target ON target.UserId = r.TargetUserId
        LEFT JOIN dbo.ChatThreads AS thread ON thread.ThreadId = r.ThreadId
        WHERE (@ActorRole = N'OWNER' AND r.RequestStatus = N'PENDING_OWNER')
           OR (r.TargetUserId = @ActorUserId
               AND r.TargetRole = @ActorRole
               AND r.RequestStatus = N'PENDING_TARGET')
           OR r.RequesterUserId = @ActorUserId
        ORDER BY CASE WHEN r.RequestStatus = N'PENDING' THEN 0 ELSE 1 END,
                 r.CreatedAt DESC, r.RequestId DESC;
        RETURN;
    END;

    IF @Action = N'REQUEST'
    BEGIN
        IF @ActorRole = N'OWNER'
            THROW 50223, 'Owners can start a chat directly.', 1;
        IF @TargetUserId IS NULL OR @TargetUserId <= 0 OR @TargetUserId = @ActorUserId
            THROW 50224, 'Choose a different active user to request.', 1;
        IF @TargetRole NOT IN (N'OWNER', N'SUPPLIER', N'CONSUMER')
            THROW 50225, 'Target role must be Owner, Supplier, or Consumer.', 1;
        IF NOT EXISTS
        (
            SELECT 1 FROM dbo.Users
            WHERE UserId = @TargetUserId
              AND IsActive = 1
              AND UPPER(LTRIM(RTRIM(UserRole))) = @TargetRole
        )
            THROW 50226, 'The selected user is not active or their role does not match.', 1;

        IF EXISTS
        (
            SELECT 1 FROM dbo.ChatThreads
            WHERE IsOwnerApproved = 1
              AND ISNULL(RelatedSupplyId, 0) = ISNULL(@RelatedSupplyId, 0)
              AND (ThreadKind <> N'PEER' OR AccessEndsAt > SYSDATETIME())
              AND
              (
                  (RequesterUserId = @ActorUserId AND ParticipantUserId = @TargetUserId)
                  OR (RequesterUserId = @TargetUserId AND ParticipantUserId = @ActorUserId)
              )
        )
           AND UPPER(LTRIM(RTRIM(COALESCE(@RequestNote, N''))))
               NOT LIKE N'ACCESS EXTENSION REQUESTED.%'
            THROW 50227, 'An approved conversation already exists for these users.', 1;

        DECLARE @PendingRequestId BIGINT;
        SELECT TOP (1) @PendingRequestId = RequestId
        FROM dbo.ChatAccessRequests WITH (UPDLOCK, HOLDLOCK)
        WHERE RequesterUserId = @ActorUserId
          AND TargetUserId = @TargetUserId
                    AND RequestStatus IN (N'PENDING_TARGET', N'PENDING_OWNER')
          AND ISNULL(RelatedSupplyId, 0) = ISNULL(@RelatedSupplyId, 0)
        ORDER BY RequestId DESC;

        IF @PendingRequestId IS NULL
        BEGIN
            INSERT INTO dbo.ChatAccessRequests
            (
                RequesterUserId, RequesterRole, TargetUserId, TargetRole,
                RelatedSupplyId, RequestNote, RequestStatus
            )
            VALUES
            (
                @ActorUserId, @ActorRole, @TargetUserId, @TargetRole,
                @RelatedSupplyId, NULLIF(LTRIM(RTRIM(@RequestNote)), N''),
                N'PENDING_TARGET'
            );
            SET @PendingRequestId = CONVERT(BIGINT, SCOPE_IDENTITY());
        END;

        SELECT
            r.RequestId,
            r.RequesterUserId,
            requester.FullName AS RequesterName,
            r.RequesterRole,
            r.TargetUserId,
            target.FullName AS TargetName,
            r.TargetRole,
            r.RelatedSupplyId,
            thread.AccessStartsAt,
            thread.AccessEndsAt,
            r.RequestNote,
            r.RequestStatus,
            r.ThreadId,
            r.CreatedAt,
            r.ReviewedByOwnerId,
            r.ReviewedAt
        FROM dbo.ChatAccessRequests AS r
        INNER JOIN dbo.Users AS requester ON requester.UserId = r.RequesterUserId
        INNER JOIN dbo.Users AS target ON target.UserId = r.TargetUserId
        LEFT JOIN dbo.ChatThreads AS thread ON thread.ThreadId = r.ThreadId
        WHERE r.RequestId = @PendingRequestId;
        RETURN;
    END;

    IF @Action IN (N'TARGETACCEPT', N'TARGETREJECT')
    BEGIN
        IF @RequestId IS NULL OR @RequestId <= 0
            THROW 50229, 'A valid chat request ID is required.', 1;

        UPDATE dbo.ChatAccessRequests
        SET RequestStatus = CASE
                WHEN @Action = N'TARGETACCEPT' THEN N'PENDING_OWNER'
                ELSE N'REJECTED'
            END
        WHERE RequestId = @RequestId
          AND TargetUserId = @ActorUserId
          AND TargetRole = @ActorRole
          AND RequestStatus = N'PENDING_TARGET';

        IF @@ROWCOUNT = 0
            THROW 50230, 'Pending chat request for this user was not found.', 1;

        SELECT
            r.RequestId,
            r.RequesterUserId,
            requester.FullName AS RequesterName,
            r.RequesterRole,
            r.TargetUserId,
            target.FullName AS TargetName,
            r.TargetRole,
            r.RelatedSupplyId,
            thread.AccessStartsAt,
            thread.AccessEndsAt,
            r.RequestNote,
            r.RequestStatus,
            r.ThreadId,
            r.CreatedAt,
            r.ReviewedByOwnerId,
            r.ReviewedAt
        FROM dbo.ChatAccessRequests AS r
        INNER JOIN dbo.Users AS requester ON requester.UserId = r.RequesterUserId
        INNER JOIN dbo.Users AS target ON target.UserId = r.TargetUserId
        LEFT JOIN dbo.ChatThreads AS thread ON thread.ThreadId = r.ThreadId
        WHERE r.RequestId = @RequestId;
        RETURN;
    END;

    IF @Action IN (N'APPROVE', N'REJECT')
    BEGIN
        IF @ActorRole <> N'OWNER'
            THROW 50228, 'Only an owner can review chat requests.', 1;
        IF @RequestId IS NULL OR @RequestId <= 0
            THROW 50229, 'A valid chat request ID is required.', 1;

        DECLARE @RequesterId BIGINT;
        DECLARE @RequesterRole NVARCHAR(20);
        DECLARE @RequestedTargetId BIGINT;
        DECLARE @RequestedTargetRole NVARCHAR(20);
        DECLARE @RequestedSupplyId BIGINT;
        DECLARE @LinkedThreadId BIGINT;

        BEGIN TRANSACTION;
            SELECT
                @RequesterId = RequesterUserId,
                @RequesterRole = RequesterRole,
                @RequestedTargetId = TargetUserId,
                @RequestedTargetRole = TargetRole,
                @RequestedSupplyId = RelatedSupplyId,
                @LinkedThreadId = ThreadId
            FROM dbo.ChatAccessRequests WITH (UPDLOCK, HOLDLOCK)
            WHERE RequestId = @RequestId
              AND RequestStatus = N'PENDING_OWNER';

            IF @RequesterId IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                THROW 50230, 'Pending chat request not found.', 1;
            END;

            IF @Action = N'APPROVE'
            BEGIN
                IF @RequestedTargetRole <> N'OWNER'
                   AND
                   (
                       @AccessStartDate IS NULL
                       OR @AccessEndDate IS NULL
                       OR @AccessStartDate < CONVERT(DATE, SYSDATETIME())
                       OR @AccessEndDate < @AccessStartDate
                   )
                    THROW 50232, 'Choose a valid chat access date range starting today or later.', 1;

                SELECT TOP (1) @LinkedThreadId = ThreadId
                FROM dbo.ChatThreads WITH (UPDLOCK, HOLDLOCK)
                WHERE IsOwnerApproved = 1
                  AND ISNULL(RelatedSupplyId, 0) = ISNULL(@RequestedSupplyId, 0)
                  AND
                  (
                      (RequesterUserId = @RequesterId AND ParticipantUserId = @RequestedTargetId)
                      OR (RequesterUserId = @RequestedTargetId AND ParticipantUserId = @RequesterId)
                  );

                IF @LinkedThreadId IS NULL
                BEGIN
                    INSERT INTO dbo.ChatThreads
                    (
                        OwnerUserId, RequesterUserId, RequesterRole,
                        ParticipantUserId, ParticipantRole, ThreadKind,
                        IsOwnerApproved, RelatedSupplyId, AccessStartsAt, AccessEndsAt
                    )
                    VALUES
                    (
                        @ActorUserId, @RequesterId, @RequesterRole,
                        @RequestedTargetId, @RequestedTargetRole,
                        CASE WHEN @RequestedTargetRole = N'OWNER' THEN N'OWNER' ELSE N'PEER' END,
                        1, @RequestedSupplyId,
                        CASE WHEN @RequestedTargetRole = N'OWNER' THEN NULL
                            ELSE CONVERT(DATETIME2(0), @AccessStartDate) END,
                        CASE WHEN @RequestedTargetRole = N'OWNER' THEN NULL
                            ELSE DATEADD(DAY, 1, CONVERT(DATETIME2(0), @AccessEndDate)) END
                    );
                    SET @LinkedThreadId = CONVERT(BIGINT, SCOPE_IDENTITY());
                END;

                IF @RequestedTargetRole <> N'OWNER'
                BEGIN
                    UPDATE dbo.ChatThreads
                    SET AccessStartsAt = CONVERT(DATETIME2(0), @AccessStartDate),
                        AccessEndsAt = DATEADD(DAY, 1, CONVERT(DATETIME2(0), @AccessEndDate)),
                        UpdatedAt = SYSDATETIME()
                    WHERE ThreadId = @LinkedThreadId
                      AND ThreadKind = N'PEER';
                END;

                UPDATE dbo.ChatAccessRequests
                SET RequestStatus = N'APPROVED',
                    ThreadId = @LinkedThreadId,
                    ReviewedByOwnerId = @ActorUserId,
                    ReviewedAt = SYSDATETIME()
                WHERE RequestId = @RequestId;
            END
            ELSE
            BEGIN
                UPDATE dbo.ChatAccessRequests
                SET RequestStatus = N'REJECTED',
                    ReviewedByOwnerId = @ActorUserId,
                    ReviewedAt = SYSDATETIME()
                WHERE RequestId = @RequestId;
            END;
        COMMIT TRANSACTION;

        SELECT
            r.RequestId,
            r.RequesterUserId,
            requester.FullName AS RequesterName,
            r.RequesterRole,
            r.TargetUserId,
            target.FullName AS TargetName,
            r.TargetRole,
            r.RelatedSupplyId,
            thread.AccessStartsAt,
            thread.AccessEndsAt,
            r.RequestNote,
            r.RequestStatus,
            r.ThreadId,
            r.CreatedAt,
            r.ReviewedByOwnerId,
            r.ReviewedAt
        FROM dbo.ChatAccessRequests AS r
        INNER JOIN dbo.Users AS requester ON requester.UserId = r.RequesterUserId
        INNER JOIN dbo.Users AS target ON target.UserId = r.TargetUserId
        LEFT JOIN dbo.ChatThreads AS thread ON thread.ThreadId = r.ThreadId
        WHERE r.RequestId = @RequestId;
        RETURN;
    END;

    THROW 50231, 'Invalid chat request action.', 1;
END;
GO