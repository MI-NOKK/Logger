using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace logger
{
    class Log : IDisposable // IDisposable 인터페이스를 구현하여 객체가 더 이상 사용되지 않을 때 리소스를 정리할 수 있도록 함
    {
        private readonly string stringFileName = "ProjectName"; // 로그 파일 이름을 저장하는 상수
        private readonly string projectName; // 로그를 남기는 프로젝트
        private bool disposed = false; // Dispose 패턴을 사용하기 위한 플래그

        private static readonly object fileLock = new object(); // 다중 스레드 환경에서 파일 접근을 동기화하기 위한 락 객체

        private const long MaxFileSize = 2 * 1024 * 1024; // 파일의 최대 크기를 2MB로 설정
        private const int MaxFileCount = 3; // 로그 파일을 3개까지만 유지

        /// <summary>
        /// 생성자, 로그 호출 위치를 인자로 받아 저장
        /// </summary>
        /// <param name="_projectName"></param>
        public Log(string _projectName)
        {
            this.projectName = _projectName; // 로그 호출 위치 정보를 저장
        }

        /// <summary>
        /// 기본 "INFO" 레벨로 메시지를 기록하는 함수
        /// </summary>
        /// <param name="message"></param>
        public void Write(string message) 
        {
            WriteToFile("INFO", message); // INFO 레벨로 로그 기록
        }

        /// <summary>
        /// 지정된 로그 레벨로 메시지를 기록하는 함수
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        public void Write(string logLevel, string message)
        {
            WriteToFile(logLevel, message); // 해당 레벨로 로그 기록
        }

        /// <summary>
        /// 한 줄로 로그를 기록하는 함수 (기본 "INFO" 레벨)
        /// </summary>
        /// <param name="message">로그에 남길 문자열</param>
        public void WriteLine(string message)
        {
            WriteToFile("INFO", message); // INFO 레벨로 로그 기록
        }

        /// <summary>
        /// 지정된 로그 레벨로 한 줄 로그를 기록하는 함수
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        public void WriteLine(string logLevel, string message)
        {
            WriteToFile(logLevel, message); // 해당 레벨로 로그 기록
        }

        /// <summary>
        /// 실제로 파일에 로그를 기록하는 함수
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        private void WriteToFile(string logLevel, string message)
        {
            CheckRollover(); // 파일 크기가 넘을 경우 파일 롤오버(교체) 실행

            lock (fileLock) // 다중 스레드에서 파일에 동시 접근하지 않도록 파일 잠금
            {
                try
                {
                    using (FileStream fileStream = new FileStream(GenerateFilename(), FileMode.Append, FileAccess.Write, FileShare.None)) // 로그 파일을 열거나 없으면 생성
                    {
                        using (StreamWriter writer = new StreamWriter(fileStream)) // 파일에 기록하기 위한 스트림 객체
                        {
                            string writeMessage = $"{logLevel}:{projectName}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}:{message}"; // 로그 메시지 형식 정의
                            writer.WriteLine(writeMessage); // 메시지 파일에 기록
                        }
                    }
                }
                catch (IOException ex) // 파일 접근 중 오류 발생 시 처리
                {
                    Console.WriteLine($"파일 접근 중 오류 발생: {ex.Message}"); // 오류 메시지 출력
                }
                catch (Exception ex) // 일반적인 예외 처리
                {
                    Console.WriteLine($"파일 접근 중 오류 발생: {ex.Message}"); // 오류 메시지 출력
                }
            }
        }

        /// <summary>
        /// 파일 크기를 확인하여 필요하면 롤오버 실행
        /// </summary>
        private void CheckRollover()
        {
            FileInfo fileInfo = new FileInfo(GenerateFilename()); // 현재 로그 파일의 정보를 가져옴

            if (fileInfo.Exists && fileInfo.Length > MaxFileSize) // 파일이 존재하고 크기가 설정된 최대값을 넘으면
            {
                RolloverFiles(); // 파일 롤오버 수행
            }
        }

        /// <summary>
        /// 로그 파일 이름을 생성하는 함수
        /// </summary>
        /// <returns></returns>
        private string GenerateFilename() 
        {

            string curDirPath = "\\log"; // 로그 파일이 저장될 경로 생성

            if (!Directory.Exists(curDirPath)) // 해당 경로가 없으면
            {
                Directory.CreateDirectory(curDirPath); // 로그 폴더를 생성
            }

            string stringPath = Path.Combine(curDirPath, this.stringFileName + ".log"); // 로그 파일 경로와 이름을 결합하여 반환

            return stringPath; // 로그 파일 경로 반환
        }

        /// <summary>
        /// 로그 디렉토리 경로를 반환하는 함수
        /// </summary>
        /// <returns></returns>
        private string GetLogDirectoryPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "linkRai", "log"); // 앱 데이터 폴더 하위에 로그 폴더 경로 생성
        }

        /// <summary>
        /// 파일 롤오버를 수행하는 함수
        /// </summary>
        private void RolloverFiles()
        {
            string curDirPath = GetLogDirectoryPath(); // 로그 디렉토리 경로를 가져옴

            lock (fileLock) // 다중 스레드 환경에서 파일 조작을 동기화
            {
                for (int i = MaxFileCount - 1; i >= 0; i--) // 파일을 최신순으로 처리하기 위해 거꾸로 반복
                {
                    string currentFile = Path.Combine(curDirPath, $"{stringFileName}_{i}.log"); // 현재 파일 이름
                    string nextFile = Path.Combine(curDirPath, $"{stringFileName}_{i + 1}.log"); // 다음 파일 이름

                    if (File.Exists(currentFile)) // 현재 파일이 존재하면
                    {
                        if (i == MaxFileCount - 1) // 마지막 파일일 경우 삭제
                        {
                            File.Delete(currentFile);
                        }
                        else // 그렇지 않으면 파일을 다음 번호로 이동
                        {
                            File.Move(currentFile, nextFile);
                        }
                    }
                }

                string originalFile = Path.Combine(curDirPath, $"{stringFileName}.log"); // 원본 로그 파일 이름
                string newFile = Path.Combine(curDirPath, $"{stringFileName}_0.log"); // 롤오버될 새로운 파일 이름

                if (File.Exists(originalFile)) // 원본 파일이 존재하면
                {
                    File.Move(originalFile, newFile); // 파일 이동
                }
            }
        }

        /// <summary>
        /// IDisposable 구현, 리소스를 정리하는 함수
        /// </summary>
        public void Dispose()
        {
            Dispose(true); // 리소스 해제
            GC.SuppressFinalize(this); // 가비지 컬렉터가 소멸자를 호출하지 않도록 함
        }

        /// <summary>
        /// 리소스 해제를 처리하는 함수
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed) // 아직 해제되지 않은 경우
            {
                if (disposing) // 관리되는 리소스를 해제하는 경우
                {
                    // 현재는 해제할 관리되는 리소스가 없음
                }

                // 관리되지 않는 리소스를 해제
                disposed = true; // 해제됨 플래그 설정
            }
        }

        /// <summary>
        /// 소멸자, Dispose를 호출하지 않고 객체가 소멸될 때 호출
        /// </summary>
        ~Log()
        {
            Dispose(false); // 관리되지 않는 리소스만 해제
        }
    }
}
