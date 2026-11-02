IF COL_LENGTH(N'dbo.ChatThreads', N'AccessStartsAt') IS NULL
    ALTER TABLE dbo.ChatThreads ADD AccessStartsAt DATETIME2(0) NULL;
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
        WHERE
          (
              (RequesterUserId = @ActorUserId AND TargetUserId = @TargetUserId)
              OR (RequesterUserId = @TargetUserId AND TargetUserId = @ActorUserId)
          )
          AND RequestStatus IN (N'PENDING_TARGET', N'PENDING_OWNER')
          AND ISNULL(RelatedSupplyId, 0) = ISNULL(@RelatedSupplyId, 0)
          AND (
              (
                  UPPER(LTRIM(RTRIM(COALESCE(@RequestNote, N''))))
                      LIKE N'ACCESS EXTENSION REQUESTED.%'
                  AND UPPER(LTRIM(RTRIM(COALESCE(RequestNote, N''))))
                      LIKE N'ACCESS EXTENSION REQUESTED.%'
              )
              OR (
                  UPPER(LTRIM(RTRIM(COALESCE(@RequestNote, N''))))
                      NOT LIKE N'ACCESS EXTENSION REQUESTED.%'
                  AND UPPER(LTRIM(RTRIM(COALESCE(RequestNote, N''))))
                      NOT LIKE N'ACCESS EXTENSION REQUESTED.%'
              )
          )
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
