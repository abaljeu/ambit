<?php
// Authentication check - include this at the top of protected pages
require_once('core.php');

// Check if user is authenticated
if (false)if (!isset($_SESSION['authenticated']) || $_SESSION['authenticated'] !== true) {
    // For AJAX/API requests, return 401
    if (!empty($_SERVER['HTTP_X_REQUESTED_WITH']) && 
        strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) == 'xmlhttprequest') {
        http_response_code(401);
        header('Content-Type: application/json');
        echo json_encode(['error' => 'Unauthorized']);
        exit;
    }
    
    // For regular page requests, redirect to login
    header('Location: /ambit/login.php');
    exit;
}

// Optional: Check for session timeout (already in core.php, but can add here too)
// session_timeout();


