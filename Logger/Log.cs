using System.Security.Cryptography.X509Certificates;

namespace Logger
{
    public class Log
    {
        static public void Main(string[] args)
        {

        }
        private readonly string logPath;
        private readonly Lock fileLock = new();
        private enum LogType
        {
            INFO,
            WARNING,
            ERROR,
            EXCEPTION
        }

        //Method Name: Logger
        //Description: Initializes the logger with a specified file name. The log file will be created in the application's base directory with
        //             a .log extension.
        //Parameters:  string message - the formatted string to send to the server.
        //Returns:     void
        public Log(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Log file name cannot be empty.", nameof(fileName));
            }

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            logPath = Path.Combine(basePath, fileName + ".log");
        }

        //Method Name: LogInfo
        //Description: writes to the log as a normal message
        //Parameters:  string message - the formatted string to send to the server.
        //Returns:     void
        public void LogInfo(string message)
        {
            Write(message, LogType.INFO);
        }

        //Method Name: LogWarn
        //Description: writes to the log as a warning message
        //Parameters:  string message - the formatted string to send to the server.
        //Returns:     void
        public void LogWarn(string message)
        {
            Write(message, LogType.WARNING);
        }

        //Method Name: LogError
        //Description: writes to the log as a error message
        //Parameters:  string message - the formatted string to send to the server.
        //Returns:     void
        public void LogError(string message)
        {
            Write(message, LogType.ERROR);
        }

        //Method Name: LogExError
        //Description: writes to the log as an exception message
        //Parameters:  string message - the formatted string to send to the server.
        //Returns:     void
        public void LogExError(string message, Exception ex)
        {
            Console.WriteLine(ex);
            Write(message, LogType.EXCEPTION);
        }

        //Method Name: Write
        //Description: Formats the log message that will be written to the log file
        //             Adds the date and time, with type and details.
        //Parameters:  string message - the formatted string to send to the server.
        //             LogType type - the type of log message being recorded.
        //Returns:     void
        private void Write(string message, LogType type)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string line = $"{timestamp} [{type}] {message}\n";

                lock (fileLock)
                {
                    File.AppendAllText(logPath, line);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to logger file.\n\n{ex.Message}");
            }
        }
    }
}
