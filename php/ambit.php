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
      <link rel="icon" href="data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><text y='.9em' font-size='90'>📝</text></svg>">
      <link rel="stylesheet" href="css/ambit.css">
   </head>
<body>
<h1 id="path" style="display: inline-block; margin-right: 10px;"><?= $filePath ?></h1>
<button id="save">Save</button>
<div id="parentContainer">
   <div id="newEditor" ></div>
   <div id="details">

   <!-- show the active cell: tag, sourceText, classes list, -->
         <input type="text" id="cellTag" name="cellTag" placeholder="Tag" />
         <input type="text" id="cellClasses" name="cellClasses" placeholder="Classes" />
         <br/>
         <textarea id="cellSourceText" name="cellSourceText" placeholder="Source Text" rows="4" cols="50"></textarea>
         <br/>
   </div>
</div>
<div id="links"></div>
<div>Message: <span id="messageElement"><?= "Ready" ?></span> </div>
<script type="module" src="dist/ambit.js">
</script>
<?php require_once('components/logout_button.php'); ?>
</body>
</html>
