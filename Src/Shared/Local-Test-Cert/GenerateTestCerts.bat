@ECHO OFF
ECHO\
ECHO   Usage:
ECHO       GenerateTestCerts.bat [Output Directory]
ECHO\
ECHO   This script generates the test keys and certificates used for testing.
ECHO   These keys are only used for local tests and are not known to any production systems.
ECHO   Therefore, there is no need to keep them a secret.
ECHO   Make sure it stays that way and do not use these keys elsewhere!

ECHO\

IF "%1"=="" (
    SET GTC_CERTS_DIR=.
) ELSE (
    SET GTC_CERTS_DIR=%1
)

SET OPENSSL_PATH=c:\Program Files\OpenSSL-Win64\bin
SET OPENSSL_EXE="%OPENSSL_PATH%\openssl.exe"

ECHO Output Directory: %GTC_CERTS_DIR%
ECHO OPEN SSL Tool:    %OPENSSL_EXE%
ECHO\

IF EXIST %GTC_CERTS_DIR% (   
   ECHO Output Directory already exists.
) ELSE (
   mkdir %GTC_CERTS_DIR%
   ECHO Output Directory created.
)

rem Output file names:
SET GTC_CA_KEY_FILE=%GTC_CERTS_DIR%\Temporal-Test-CA.key.pem
SET GTC_CA_CERT_FILE=%GTC_CERTS_DIR%\Temporal-Test-CA.crt.pem

SET GTC_SERVER_KEY_FILE=%GTC_CERTS_DIR%\Temporal-Test-Server.key.pem
SET GTC_SERVER_SIGN_REQ_FILE=%GTC_CERTS_DIR%\Temporal-Test-Server.csr.pem
SET GTC_SERVER_CERT_FILE=%GTC_CERTS_DIR%\Temporal-Test-Server.crt.pem

SET GTC_CLIENT_KEY_FILE=%GTC_CERTS_DIR%\Temporal-Test-Client.key.pem
SET GTC_CLIENT_SIGN_REQ_FILE=%GTC_CERTS_DIR%\Temporal-Test-Client.csr.pem
SET GTC_CLIENT_CERT_FILE=%GTC_CERTS_DIR%\Temporal-Test-Client.crt.pem
SET GTC_CLIENT_SSLCERT_FILE=%GTC_CERTS_DIR%\Temporal-Test-Client.pfx

ECHO\
ECHO Generating a private key and a certificate for a test Certificate Authority...

%OPENSSL_EXE% genrsa -out %GTC_CA_KEY_FILE% 4096
%OPENSSL_EXE% req -new -x509 -key %GTC_CA_KEY_FILE% -sha256 -subj "/C=US/ST=WA/O=Temporal Technologies Inc." -days 365 -out %GTC_CA_CERT_FILE%

ECHO %GTC_CA_KEY_FILE%
ECHO %GTC_CA_CERT_FILE%

ECHO\
ECHO Generating a private key and a certificate for Server...
%OPENSSL_EXE% genrsa -out %GTC_SERVER_KEY_FILE% 4096
%OPENSSL_EXE% req -new -key %GTC_SERVER_KEY_FILE% -out %GTC_SERVER_SIGN_REQ_FILE% -config CreateServerCert.conf
%OPENSSL_EXE% x509 -req -in %GTC_SERVER_SIGN_REQ_FILE% --CA %GTC_CA_CERT_FILE% -CAkey %GTC_CA_KEY_FILE% -CAcreateserial -out %GTC_SERVER_CERT_FILE% -days 365 -sha256 -extfile CreateServerCert.conf -extensions req_ext

ECHO\
ECHO %GTC_SERVER_KEY_FILE%
ECHO %GTC_SERVER_SIGN_REQ_FILE%
ECHO %GTC_SERVER_CERT_FILE%

ECHO\
ECHO Generating a private key and a certificate for Clients...

%OPENSSL_EXE% genrsa -out %GTC_CLIENT_KEY_FILE% 4096
%OPENSSL_EXE% req -new -key %GTC_CLIENT_KEY_FILE% -out %GTC_CLIENT_SIGN_REQ_FILE% -config CreateClientCert.conf
%OPENSSL_EXE% x509 -req -in %GTC_CLIENT_SIGN_REQ_FILE% -CA %GTC_CA_CERT_FILE% -CAkey %GTC_CA_KEY_FILE% -CAcreateserial -out %GTC_CLIENT_CERT_FILE% -days 365 -sha256 -extfile CreateClientCert.conf -extensions req_ext

ECHO\
ECHO %GTC_CLIENT_KEY_FILE%
ECHO %GTC_CLIENT_SIGN_REQ_FILE%
ECHO %GTC_CLIENT_CERT_FILE%

ECHO\
ECHO Exporting Client certs to an UNENCRYPTED (!) PFX container...

%OPENSSL_EXE% pkcs12 -export -out %GTC_CLIENT_SSLCERT_FILE% -inkey %GTC_CLIENT_KEY_FILE% -in %GTC_CLIENT_CERT_FILE% -keypbe NONE -certpbe NONE -passout pass:

ECHO\
ECHO %GTC_CLIENT_SSLCERT_FILE%

ECHO\
ECHO All done. Good bye.
ECHO\
ECHO ON
