<?php 

$servername = "localhost";
$dbuser = "root";
$dbpass = "";
$database = "camelotdb";
$dsn = "mysql:host=$servername;dbname=$database";
$secret = "ENTER_HASHING_SECRET_HERE";

function Query($query, $fields, $where, $fetch = false)
{
	global $servername;
	global $dbuser;
	global $dbpass;
	global $database;
	global $dsn;
	global $secret;
	
	if($where != null)
	{
		$query = "$query WHERE ";
		foreach(array_keys($where) as $key) 
		{
			$query = "$query $key=:$key";
			if(array_keys($where)[count($where)-1] != $key) $query = "$query,";
		}
	}
	
	$query = "$query;\n";
	
	$conn = null;
	try 
	{
		$conn = new PDO($dsn, $dbuser, $dbpass);
		
		// set the PDO error mode to exception
		$conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
		$stmt = $conn->prepare($query);
		
		foreach([$fields, $where] as $set)
			if($set != null)
				foreach(array_keys($set) as $key) 
					$stmt->bindParam($key, $set[$key]);
		
		
		$stmt->execute();
		if($fetch) return $stmt->fetchAll();
	}
	// Handle query exceptions.
	catch(PDOException $e) 
	{
		echo  $e->getMessage();
	}
	$conn = null;
}

function Select($tableName, $fields, $where)
{	
	$fieldStr = implode(", ", $fields);
	$query = "SELECT $fieldStr FROM $tableName";
	
	return Query($query, null, $where, true);
}

function Insert($tableName, $fields)
{
	$fieldStr = implode(", ", array_keys($fields));
	$valueStr = implode(", ", preg_filter('/^/', ':', array_keys($fields)));
	$query = "INSERT INTO $tableName ($fieldStr) VALUES ($valueStr)";
	
	Query($query, $fields, null);
}

function Update($tableName, $fields, $where)
{
	$valueStr = "";
	foreach(array_keys($fields) as $key) 
	{
		$valueStr = "$valueStr $key=:$key";
		if(array_keys($fields)[count($fields)-1] != $key) $valueStr = "$valueStr,";
	}
	
	$query = "UPDATE $tableName SET $valueStr";
	Query($query, $fields, $where, false);
}

switch(key($_GET))
{
	case "register":	
		if(!empty($_POST["username"]) || !empty($_POST["password"]) || !empty($_POST["mac"]))
		{
			$userFound = count(Select("users", ["name"], ["name" => $_POST["username"]])) > 0;
			if($userFound) die("1: Username already exists.");
			{
				Insert("users", ["name" => $_POST["username"], 
								 "password" => password_hash($_POST["password"], PASSWORD_DEFAULT), 
								 "mac" => $_POST["mac"], 
								 "last_login" => "CURRENT_TIMESTAMP"]);
								
				die("0: Succesfully created user.");
			}
		} else die("-1: Server connection error");
		break;
	case "getHash":
		print(Select("users", ["loginHash"], ["name" => $_POST["username"]])[0]["loginHash"]);
		
		break;
	case "verify":
		$user = Select("users", ["mac", "last_login"], ["name" => $_POST["username"]]);
		if(count($user) <= 0) die("1: Error fetching user data");
		else 
		{
			$user = $user[0];
			$raw_secret = $secret.$user["username"].$secret.$user["mac"].$user["last_login"];
			
			if(password_verify($raw_secret, $_POST["secret"]))
			{
				print("0: valid login");
			} else {
				print("2: Invalid secret");	
			}
		}
		break;
		
	case "login":
		if(empty($_POST["password"])) print("No password entered");
		else if(empty($_POST["username"])) print("No username entered");
		else if(empty($_POST["mac"])) print("Malicious form interception, revoking access...");
		else if(!empty($_POST["username"]) && !empty($_POST["password"]))
		{
			$user = Select("users", ["name", "last_login"], ["name" => $_POST["username"]]);
			if(count($user) > 0) 
			{
				$fetchedPass = Select("users", ["password"], ["name" => $_POST["username"]])[0]["password"];

				if(password_verify($_POST["password"], $fetchedPass))
				{
					$clientHash = password_hash($secret.$_POST["username"].$secret.$_POST["mac"].$user[0]["last_login"], PASSWORD_DEFAULT);
					Update("users", ["loginHash" => $clientHash], ["name" => $_POST["username"]]);
					
					print("0:".$clientHash);

				} else {
					print("Invalid password");	
				}
			}

		} else print("An error occured during the login procedure");
		break;
}

?>