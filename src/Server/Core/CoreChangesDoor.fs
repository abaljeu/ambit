namespace Gambol.Server

/// Data door into Core: mailbox host, Caller, and write policy.
type CoreChangesDoor =
    { host: MailboxHost
      caller: Caller
      writable: bool }
