<?php
require_once('core.php');

// Get the document name from the query parameter
$DOC = GET('doc') . '.amb'; // Assuming you want to append 'amb' to the document name
$filePath = "doc/$DOC"; // Relative path to the file

// // Handle POST request to save the content
// if ($_SERVER['REQUEST_METHOD'] === 'POST') {
//     // Get the JSON data from the request
//     $data = json_decode(file_get_contents('php://input'), true);
    
//     // Check if content is provided
//     if (isset($data['content'])) {
//         // Write the content to the file
//         if (file_put_contents($filePath, $data['content']) !== false) {
//             http_response_code(200); // Success
//             echo json_encode(["message" => "File saved successfully."]);
//         } else {
//             http_response_code(500); // Internal Server Error
//             echo json_encode(["message" => "Failed to save the file."]);
//         }
//     } else {
//         http_response_code(400);ff // Bad Request
//         echo json_encode(["message" => "No content provided."]);
//     }
//     exit; // Stop further execution
// }

// // Handle GET request to load the content
// if (file_exists($filePath)) {
//    //  header('Content-Type: text/plain');
//     $content = htmlspecialchars(file_get_contents($filePath));
//     $error = $filePath;
// } else {
//    http_response_code(404); // Not Found
//    $content = ''; // Set default content
//    $error = htmlspecialchars( $filePath) . " File not found.";
// }
//////////////////////////////////////////////////////////////////
?>

<html>
   <head>
      <title><?= $filePath ?></title>
   </head>
<body>
<div><?= $filePath ?> </div>
<textarea id="editor" style="height: 80%;width:100%;"><?= $content ?></textarea> 
<button id="save">Save</button>  
<div id="links"></div>
<div>Message: <span id="messageElement"><?= "Ready" ?></span> </div>
<script src="dist/ambit.js">
</script>
</body>
</html>
