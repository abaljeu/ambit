<?php
require_once('auth.php');
require_once('core.php');

// Get the document name from the query parameter
$DOC = GET('doc'); // Use the doc parameter as-is
$filePath = "doc/$DOC"; // Relative path to the file

// Content will be loaded by JavaScript (ambit.js)
$content = ''; // Empty initially, JavaScript will populate via GET request
//////////////////////////////////////////////////////////////////
?>

<html>
   <head>
      <title><?= $filePath ?></title>
   </head>
<body>
<h1 id="path" style="display: inline-block; margin-right: 10px;"><?= $filePath ?></h1>
<button id="save">Save</button>  
<textarea id="editor" style="height: 80%;width:100%;"><?= $content ?></textarea> 
<div id="links"></div>
<div>Message: <span id="messageElement"><?= "Ready" ?></span> </div>
<script type="module" src="dist/ambit.js">
</script>
<?php require_once('components/logout_button.php'); ?>
</body>
</html>
