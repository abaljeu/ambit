<?php
/**
 * Reverse proxy for /ambit -> Azure Web App
 * Used when mod_proxy is unavailable (e.g. Hostgator shared hosting)
 */

$BACKEND = 'https://amble-f6bfcfdygjd9b6b7.canadacentral-01.azurewebsites.net';
$MOUNT = '/ambit';

$path = isset($_GET['path']) ? $_GET['path'] : '';
$path = $path === '' ? '' : $path;  // /ambit -> '', /ambit/login -> /login, /ambit/amble/state -> /amble/state

// Build backend URL (Azure app runs at root: /login, /amble, /amble/state, /amble/changes, /logout)
// /ambit alone -> /amble (entry point)
$backendPath = ($path === '' || $path === '/') ? '/amble' : $path;
$backendUrl = rtrim($BACKEND, '/') . $backendPath;
if (!empty($_GET)) {
    $params = $_GET;
    unset($params['path']);
    if (!empty($params)) {
        $backendUrl .= '?' . http_build_query($params);
    }
}

// Headers to forward (skip Host - backend needs its own)
$forwardHeaders = ['Accept', 'Accept-Language', 'Accept-Encoding', 'Content-Type', 'Cookie', 'Authorization', 'X-Requested-With'];
$headers = [];
foreach ($forwardHeaders as $h) {
    $key = 'HTTP_' . strtoupper(str_replace('-', '_', $h));
    if (!empty($_SERVER[$key])) {
        $headers[] = $h . ': ' . $_SERVER[$key];
    }
}

$ch = curl_init($backendUrl);
curl_setopt_array($ch, [
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_HEADER => true,
    CURLOPT_FOLLOWLOCATION => false,  // We'll rewrite Location headers
    CURLOPT_TIMEOUT => 60,
    CURLOPT_CUSTOMREQUEST => $_SERVER['REQUEST_METHOD'],
    CURLOPT_HTTPHEADER => $headers,
]);

// Forward request body for POST/PUT/PATCH
if (in_array($_SERVER['REQUEST_METHOD'], ['POST', 'PUT', 'PATCH'])) {
    $body = file_get_contents('php://input');
    curl_setopt($ch, CURLOPT_POSTFIELDS, $body);
}

$response = curl_exec($ch);
$errno = curl_errno($ch);
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$headerSize = curl_getinfo($ch, CURLINFO_HEADER_SIZE);
curl_close($ch);

if ($errno) {
    http_response_code(502);
    header('Content-Type: text/plain');
    echo 'Proxy error: ' . curl_strerror($errno);
    exit;
}

// Split headers and body
$headerBlock = substr($response, 0, $headerSize);
$body = substr($response, $headerSize);

// Parse and forward response headers (rewrite Location for same-origin redirects)
$baseUrl = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on' ? 'https' : 'http') . '://' . $_SERVER['HTTP_HOST'];
// These headers are invalidated by curl reassembling the chunked response body.
// content-encoding is NOT skipped — curl passes the compressed body through
// unchanged, so the browser still needs that header to decompress it.
$skipHeaders = ['transfer-encoding', 'content-length'];

foreach (explode("\r\n", $headerBlock) as $line) {
    if (stripos($line, 'HTTP/') === 0) {
        http_response_code($httpCode);
        continue;
    }
    if (strpos($line, ':') === false) continue;
    list($name, $value) = explode(':', $line, 2);
    $value = trim($value);
    if (in_array(strtolower(trim($name)), $skipHeaders)) continue;
    // Rewrite Location header so redirects stay on our domain under /ambit
    if (stripos($name, 'Location') === 0) {
        if (preg_match('#^/#', $value)) {
            $value = $MOUNT . $value;  // /login -> /ambit/login
        } else {
            $value = preg_replace('#^https?://[^/]+/#', $baseUrl . $MOUNT . '/', $value);
        }
    }
    header($name . ': ' . $value, false);
}

echo $body;
