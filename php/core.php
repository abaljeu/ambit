<?php
session_start();
ini_set('error_reporting', E_ALL & ~E_DEPRECATED & ~E_NOTICE);

if (!isset($_SESSION['year'])) 
{
	$d = new DateTime();
	$d->add(new DateInterval("P5M")); // add 3 months to get next year if testing late in year
	$_SESSION['year'] = $d -> format('Y');
}

function GET($p)
{
	if (isset($_GET[$p])) return $_GET[$p];
	
	return false;
}
function POST($p)
{
	if (isset($_POST[$p])) return $_POST[$p];
	return false;
}

function SESS($p)
{
	if (isset($_SESSION[$p])) return $_SESSION[$p];
	return false;
}

// Unified error responder: sets HTTP status and returns JSON error body, then exits.
function respond_error(int $code, $message, array $context = []) : void {
	http_response_code($code);
	header('Content-Type: application/json; charset=utf-8');
	
	$error = ['error' => $message];
	if (!empty($context)) {
		$error = array_merge($error, $context);
	}
	
	echo json_encode($error, JSON_UNESCAPED_UNICODE);
	exit;
}

function session_timeout()
{
	$time = $_SERVER['REQUEST_TIME'];
	$TIMEOUT_DURATION = 3600;

	$last = SESS('LAST_ACTIVITY');


	if ($last && $time - $last > $TIMEOUT_DURATION)
	{
		$_SESSION['error'] = 'Time Out, logging out';
		$_SESSION['LAST_ACTIVITY'] = $time;
		return true;	
	}
	//echo $time -  $last;
	$_SESSION['LAST_ACTIVITY'] = $time;
	return false;
}
//if (session_timeout()) return;
