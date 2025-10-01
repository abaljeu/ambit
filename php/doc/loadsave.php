<?php
require_once('../auth.php');

header("Access-Control-Allow-Origin: http://localho.st:5500");
header("Access-Control-Allow-Methods: GET, POST, OPTIONS");
header("Access-Control-Allow-Headers: Content-Type"); 

error_log("loadsave.php received: " . $_SERVER['REQUEST_URI']);

ob_implicit_flush(1);
header("Content-Type: text/plain");

// Handle preflight OPTIONS request quickly
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

$doc  = $_GET['doc'] ?? '';
error_log("loadsave.php params: " . $doc);

if ($doc === 'loadsave.php') {
    http_response_code(400);
    exit("Refusing to overwrite myself");
}

if ($doc === '') {
    http_response_code(400);
    exit;
}
$body = file_get_contents("php://input");

if ($_SERVER['REQUEST_METHOD'] === 'POST' && $doc) {
    $safe = basename($doc);
    $path = __DIR__ . "/" . $safe;

    //echo "Saved Doc\n";
    if (file_put_contents($path, $body) !== false) {
        http_response_code(200);
        flush();
        exit;
    } else {
        http_response_code(500);
        echo "Write failed";
        exit;
    }
}

if ($_SERVER['REQUEST_METHOD'] === 'GET' && $doc) {
    $safe = basename($doc);
    $path = __DIR__ . "/" . $safe;

    if (file_exists($path)) {
        readfile($path);
        flush();
        exit;
    } else {
        http_response_code(404);
        echo "Not found";
        flush();
        exit;
    }
}

http_response_code(400);
echo "Bad request";
