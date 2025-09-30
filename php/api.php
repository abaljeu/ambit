<?php
// API proxy - handles GET and POST requests to remote endpoint with authentication
require_once('core.php');
require_once('config.php');
require_once('auth.php'); // Requires authentication

// Get the document path from query parameter
$filePath = GET('doc');

if (!$filePath) {
    respond_error(400, 'Missing doc parameter');
}

// Construct the full API URL
$apiUrl = API_BASE_URL . $filePath;

// Handle GET request - load document
if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    // Create context with authentication header
    $options = [
        'http' => [
            'method' => 'GET',
            'header' => "Authorization: Bearer " . API_SECRET . "\r\n"
        ]
    ];
    $context = stream_context_create($options);
    
    // Fetch from remote API
    $content = @file_get_contents($apiUrl, false, $context);
    
    if ($content === false) {
        $error = error_get_last();
        respond_error(404, 'File not found or unable to load', ['path' => $filePath]);
    }
    
    // Return the content as plain text
    header('Content-Type: text/plain; charset=utf-8');
    echo $content;
    exit;
}

// Handle POST request - save document
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    // Get the content from the request body
    $content = file_get_contents('php://input');
    
    if ($content === false || $content === '') {
        respond_error(400, 'No content provided');
    }
    
    // Create context with authentication header
    $options = [
        'http' => [
            'method' => 'POST',
            'header' => "Authorization: Bearer " . API_SECRET . "\r\n" .
                       "Content-Type: text/plain\r\n",
            'content' => $content
        ]
    ];
    $context = stream_context_create($options);
    
    // Send to remote API
    $response = @file_get_contents($apiUrl, false, $context);
    
    if ($response === false) {
        $error = error_get_last();
        respond_error(500, 'Failed to save document', ['path' => $filePath]);
    }
    
    // Return success response
    header('Content-Type: text/plain; charset=utf-8');
    echo "Saved Doc";
    exit;
}

// Method not allowed
respond_error(405, 'Method not allowed');


