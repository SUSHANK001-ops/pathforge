IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [ProfilePicture] nvarchar(max) NULL,
    [Bio] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [ChatMessages] (
    [Id] int NOT NULL IDENTITY,
    [MessageText] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [SentAt] datetime2 NOT NULL,
    [SenderId] int NOT NULL,
    [ReceiverId] int NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatMessages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChatMessages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Courses] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ThumbnailUrl] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [EducatorId] int NOT NULL,
    CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Courses_Users_EducatorId] FOREIGN KEY ([EducatorId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Assignments] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [MaxMarks] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CourseId] int NOT NULL,
    CONSTRAINT [PK_Assignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assignments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CourseEnrolments] (
    [Id] int NOT NULL IDENTITY,
    [EnrolledAt] datetime2 NOT NULL,
    [StudentId] int NOT NULL,
    [CourseId] int NOT NULL,
    CONSTRAINT [PK_CourseEnrolments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseEnrolments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CourseEnrolments_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [DiscussionPosts] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(300) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [IsFlagged] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ParentPostId] int NULL,
    [CourseId] int NOT NULL,
    [AuthorId] int NOT NULL,
    CONSTRAINT [PK_DiscussionPosts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiscussionPosts_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DiscussionPosts_DiscussionPosts_ParentPostId] FOREIGN KEY ([ParentPostId]) REFERENCES [DiscussionPosts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DiscussionPosts_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Modules] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Type] nvarchar(30) NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CourseId] int NOT NULL,
    CONSTRAINT [PK_Modules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Modules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Quizzes] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [TimeLimitMinutes] int NOT NULL,
    [PassMarkPercent] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CourseId] int NOT NULL,
    CONSTRAINT [PK_Quizzes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Quizzes_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AssignmentSubmissions] (
    [Id] int NOT NULL IDENTITY,
    [FileUrl] nvarchar(max) NULL,
    [Comment] nvarchar(max) NULL,
    [Status] nvarchar(20) NOT NULL,
    [MarksAwarded] int NULL,
    [GraderFeedback] nvarchar(max) NULL,
    [SubmittedAt] datetime2 NOT NULL,
    [GradedAt] datetime2 NULL,
    [AssignmentId] int NOT NULL,
    [StudentId] int NOT NULL,
    CONSTRAINT [PK_AssignmentSubmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssignmentSubmissions_Assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [Assignments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AssignmentSubmissions_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Materials] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [FileUrl] nvarchar(max) NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    [ModuleId] int NOT NULL,
    CONSTRAINT [PK_Materials] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Materials_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [StudentProgress] (
    [Id] int NOT NULL IDENTITY,
    [Status] nvarchar(20) NOT NULL,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [StudentId] int NOT NULL,
    [ModuleId] int NOT NULL,
    CONSTRAINT [PK_StudentProgress] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentProgress_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StudentProgress_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuizAttempts] (
    [Id] int NOT NULL IDENTITY,
    [ScorePercent] int NOT NULL,
    [CorrectAnswers] int NOT NULL,
    [TotalQuestions] int NOT NULL,
    [Passed] bit NOT NULL,
    [AttemptedAt] datetime2 NOT NULL,
    [QuizId] int NOT NULL,
    [StudentId] int NOT NULL,
    CONSTRAINT [PK_QuizAttempts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuizAttempts_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuizAttempts_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuizQuestions] (
    [Id] int NOT NULL IDENTITY,
    [QuestionText] nvarchar(max) NOT NULL,
    [Marks] int NOT NULL,
    [DisplayOrder] int NOT NULL,
    [QuizId] int NOT NULL,
    CONSTRAINT [PK_QuizQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuizQuestions_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [QuizOptions] (
    [Id] int NOT NULL IDENTITY,
    [OptionText] nvarchar(max) NOT NULL,
    [IsCorrect] bit NOT NULL,
    [QuestionId] int NOT NULL,
    CONSTRAINT [PK_QuizOptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuizOptions_QuizQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [QuizQuestions] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Assignments_CourseId] ON [Assignments] ([CourseId]);

CREATE INDEX [IX_AssignmentSubmissions_AssignmentId] ON [AssignmentSubmissions] ([AssignmentId]);

CREATE INDEX [IX_AssignmentSubmissions_StudentId] ON [AssignmentSubmissions] ([StudentId]);

CREATE INDEX [IX_ChatMessages_ReceiverId] ON [ChatMessages] ([ReceiverId]);

CREATE INDEX [IX_ChatMessages_SenderId] ON [ChatMessages] ([SenderId]);

CREATE INDEX [IX_CourseEnrolments_CourseId] ON [CourseEnrolments] ([CourseId]);

CREATE INDEX [IX_CourseEnrolments_StudentId] ON [CourseEnrolments] ([StudentId]);

CREATE INDEX [IX_Courses_EducatorId] ON [Courses] ([EducatorId]);

CREATE INDEX [IX_DiscussionPosts_AuthorId] ON [DiscussionPosts] ([AuthorId]);

CREATE INDEX [IX_DiscussionPosts_CourseId] ON [DiscussionPosts] ([CourseId]);

CREATE INDEX [IX_DiscussionPosts_ParentPostId] ON [DiscussionPosts] ([ParentPostId]);

CREATE INDEX [IX_Materials_ModuleId] ON [Materials] ([ModuleId]);

CREATE INDEX [IX_Modules_CourseId] ON [Modules] ([CourseId]);

CREATE INDEX [IX_QuizAttempts_QuizId] ON [QuizAttempts] ([QuizId]);

CREATE INDEX [IX_QuizAttempts_StudentId] ON [QuizAttempts] ([StudentId]);

CREATE INDEX [IX_QuizOptions_QuestionId] ON [QuizOptions] ([QuestionId]);

CREATE INDEX [IX_QuizQuestions_QuizId] ON [QuizQuestions] ([QuizId]);

CREATE INDEX [IX_Quizzes_CourseId] ON [Quizzes] ([CourseId]);

CREATE INDEX [IX_StudentProgress_ModuleId] ON [StudentProgress] ([ModuleId]);

CREATE INDEX [IX_StudentProgress_StudentId] ON [StudentProgress] ([StudentId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260908162736_InitialCreate', N'10.0.11');

COMMIT;
GO

