// Global JavaScript error logging
(function() {
    'use strict';

    // Configuration
    const CONFIG = {
        logEndpoint: '/Error/LogJavaScriptError',
        enableConsoleLog: true,
        maxStackTraceLength: 2000,
        maxErrorsPerSession: 50,
        errorCountKey: 'jsErrorCount',
        // ? NEW: Ignore patterns for development tools
        ignorePatterns: [
            'browserLink',
            'browserLinkSignalR',
            '_vs/browserLink',
            'aspnetcore-browser-refresh',
            '_framework/aspnetcore-browser-refresh'
        ]
    };

    let errorCount = parseInt(sessionStorage.getItem(CONFIG.errorCountKey) || '0');

    // ? NEW: Check if error should be ignored (development tools)
    function shouldIgnoreError(source, message) {
        const sourceStr = (source || '').toLowerCase();
        const messageStr = (message || '').toLowerCase();
        
        return CONFIG.ignorePatterns.some(pattern => 
            sourceStr.includes(pattern.toLowerCase()) || 
            messageStr.includes(pattern.toLowerCase())
        );
    }

    // Function to send error to server
    function sendErrorToServer(errorData) {
        // ? NEW: Skip ignored errors
        if (shouldIgnoreError(errorData.source, errorData.message)) {
            if (CONFIG.enableConsoleLog) {
                console.debug('Ignoring development tool error:', errorData.source);
            }
            return;
        }

        if (errorCount >= CONFIG.maxErrorsPerSession) {
            console.warn('Maximum JavaScript errors per session reached');
            return;
        }

        errorCount++;
        sessionStorage.setItem(CONFIG.errorCountKey, errorCount.toString());

        try {
            fetch(CONFIG.logEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify(errorData)
            }).catch(err => {
                if (CONFIG.enableConsoleLog) {
                    console.error('Failed to send error to server:', err);
                }
            });
        } catch (e) {
            if (CONFIG.enableConsoleLog) {
                console.error('Error in sendErrorToServer:', e);
            }
        }
    }

    // Enhanced error data collection
    function createErrorData(message, source, line, column, error, type = 'javascript') {
        const errorData = {
            message: message ? message.toString().substring(0, 500) : 'Unknown error',
            source: source || window.location.href,
            line: line || 0,
            column: column || 0,
            stack: error && error.stack ? 
                   error.stack.toString().substring(0, CONFIG.maxStackTraceLength) : 
                   'No stack trace available',
            url: window.location.href,
            userAgent: navigator.userAgent,
            timestamp: new Date().toISOString(),
            type: type,
            userId: window.currentUserId || 'anonymous', // Set this in layout if user is logged in
            sessionId: window.sessionId || 'unknown' // Set this in layout
        };

        // Add additional browser context
        errorData.browserInfo = {
            language: navigator.language,
            platform: navigator.platform,
            cookieEnabled: navigator.cookieEnabled,
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight,
            screenWidth: screen.width,
            screenHeight: screen.height
        };

        return errorData;
    }

    // Global error handler
    window.onerror = function(message, source, line, column, error) {
        const errorData = createErrorData(message, source, line, column, error);
        
        if (CONFIG.enableConsoleLog) {
            console.error('JavaScript Error Caught:', errorData);
        }

        sendErrorToServer(errorData);
        
        // Don't prevent default error handling
        return false;
    };

    // Unhandled promise rejection handler
    window.addEventListener('unhandledrejection', function(event) {
        const errorData = createErrorData(
            event.reason ? event.reason.toString() : 'Unhandled promise rejection',
            window.location.href,
            0,
            0,
            event.reason,
            'unhandledrejection'
        );

        if (CONFIG.enableConsoleLog) {
            console.error('Unhandled Promise Rejection:', errorData);
        }

        sendErrorToServer(errorData);
    });

    // Console error override (optional - captures console.error calls)
    if (CONFIG.enableConsoleLog && typeof console !== 'undefined' && console.error) {
        const originalConsoleError = console.error;
        console.error = function() {
            // Call original console.error
            originalConsoleError.apply(console, arguments);
            
            // Log to server if it looks like an actual error
            const message = Array.from(arguments).join(' ');
            if (message && message.length > 10) { // Avoid logging trivial messages
                const errorData = createErrorData(
                    message,
                    window.location.href,
                    0,
                    0,
                    new Error(message),
                    'console'
                );
                
                sendErrorToServer(errorData);
            }
        };
    }

    // Manual error reporting function for specific application errors
    window.logApplicationError = function(message, context, severity = 'error') {
        const errorData = createErrorData(
            message,
            window.location.href,
            0,
            0,
            new Error(message),
            'application'
        );
        
        errorData.severity = severity;
        errorData.context = context;

        if (CONFIG.enableConsoleLog) {
            // ? FIX: Validate severity is a valid console method
            const validMethods = ['error', 'warn', 'info', 'log', 'debug'];
            const consoleMethod = validMethods.includes(severity) ? severity : 'error';
            console[consoleMethod]('Application Error:', errorData);
        }

        sendErrorToServer(errorData);
    };

    // Network error monitoring (for AJAX requests)
    if (typeof fetch !== 'undefined') {
        const originalFetch = window.fetch;
        window.fetch = function() {
            return originalFetch.apply(this, arguments)
                .catch(error => {
                    const errorData = createErrorData(
                        `Fetch error: ${error.message}`,
                        arguments[0] ? arguments[0].toString() : 'unknown',
                        0,
                        0,
                        error,
                        'network'
                    );

                    if (CONFIG.enableConsoleLog) {
                        console.error('Network Error:', errorData);
                    }

                    sendErrorToServer(errorData);
                    
                    // Re-throw the error
                    throw error;
                });
        };
    }

    // jQuery AJAX error handler (if jQuery is present)
    document.addEventListener('DOMContentLoaded', function() {
        if (typeof $ !== 'undefined' && $.ajaxSetup) {
            $(document).ajaxError(function(event, jqXHR, ajaxSettings, thrownError) {
                const errorData = createErrorData(
                    `AJAX error: ${thrownError || jqXHR.statusText}`,
                    ajaxSettings.url,
                    0,
                    0,
                    new Error(thrownError || jqXHR.statusText),
                    'ajax'
                );

                errorData.ajaxInfo = {
                    status: jqXHR.status,
                    statusText: jqXHR.statusText,
                    responseText: jqXHR.responseText ? jqXHR.responseText.substring(0, 500) : '',
                    requestType: ajaxSettings.type,
                    requestUrl: ajaxSettings.url
                };

                if (CONFIG.enableConsoleLog) {
                    console.error('AJAX Error:', errorData);
                }

                sendErrorToServer(errorData);
            });
        }
    });

    // Page visibility change logging (for context)
    document.addEventListener('visibilitychange', function() {
        if (document.visibilityState === 'hidden') {
            // User is leaving the page, send any remaining errors
            if (errorCount > 0) {
                if (CONFIG.enableConsoleLog) {
                    console.log(`Page becoming hidden. Total JS errors this session: ${errorCount}`);
                }
            }
        }
    });

    // Expose configuration for customization
    window.JSErrorLogger = {
        config: CONFIG,
        logError: window.logApplicationError,
        getErrorCount: () => errorCount
    };

    if (CONFIG.enableConsoleLog) {
        console.log('JavaScript Error Logger initialized');
    }

})();