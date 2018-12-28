
Thank you for supporting my development.

I have included examples under "/uPromise/Examples/...", which will help you get started with the library.

For more info on this asset you can visit the product page:
http://stephenlautier.blogspot.com/p/upromise.html

If you need support contact me on:
email: stephen.lautier@outlook.com

=================
 Documentation
=================

Promise: a proxy object for a result that is initially unknown.
Deferred: the creator of the promise, and will change the result of the promise once the operation has been completed.

What are they used for?
 - They replace tradition callbacks e.g. http.Get(onDone, onFail)
 - Commonly used with data services for HTTP, but can used in any async form.
 - Promises can mitigate the "Pyramid of Doom", meaning having nested lambdas in each other.

FAQ:
 Q: I'm resolving/rejecting a promise and the .Fail/.Then on the subscribers are not being invoked.
 A: You are resolving the deferred before returning the promise, make sure that the "Resolving" will be done after (async) returning the promise or see "Creating a Promise (resolved) from non-async" if needed.

---------------------------------
 Creating a simple Deferred<T>
---------------------------------
// Create a new deferred.
var deferred = new Deferred<Person>();

// Call an async operation, and pass deferred as parameter.
// after async operation has been complete invoke either deferred.Resolve(new Person{ Name = "Joe"}) or deferred.Reject();
// IMPORTANT NOTE: if the operation is not async see: "Creating a Promise from non-async"
// THE HttpClient IS NOT INCLUDED WITHIN THIS PACKAGE - ITS JUST AN EXAMPLE.
HttpClient.Get<Person>(onDone: response => deferred.Resolve(response.Content), onFail: error => deferred.Reject(error)  

// Return the Promise.
return deferred.Promise;

-----------------------------
 Resolving a deferred
-----------------------------
// Once the async operation finishes, Simply Resolve by calling: 
deferred.Resolve()
OR
deferred.Resolve(arg) e.g. deferred.Resolve(new Person{Name = "Joe"});

After resolving the deferred, it will cause the promise to fire the .Then for all its subscribers and the "arg" value will be passed as a parameter to it.

-----------------------------
 Rejecting a deferred
-----------------------------
// Once the async operation finishes, Simply Reject by calling: 
deferred.Reject() //when no reason need to be returned and just cause the promise to fail.
OR
deferred.Reject(error) e.g. deferred.Reject(System.Net.HttpStatusCode.BadRequest);

After rejecting the deferred, it will cause the promise to fire the .Fail for all its subscribers and the "error" value will be passed as a parameter to it.

-----------------------------
 Notify a promise
-----------------------------
// Once the async operation is going, Simply Notify by calling: 
deferred.Notify(arg) e.g. deferred.Notify("enquiring data");

After notify the promise, it will cause the promise to fire the .Progress for all its subscribers and the "arg" value will be passed as a parameter to it.

------------------------------------------------
 Creating a Promise (resolved) from non-async
------------------------------------------------

PromiseFactory.StartNew(x => new Person{ Name = "Joe"});
OR 
// create a promise with delay (seconds), which will be resolved after the delay parameter.
PromiseFactory.StartNew(x => new Person{ Name = "Joe"}, delay);

------------------------------------------------
 Creating a Promise (rejected) from non-async
------------------------------------------------

return PromiseFactory.StartNewDeferred<T>(dfd =>
{
	dfd.Reject(reason);
});

OR 

// NEW IN 1.4.0!
return Promise.Reject(PlayerQuestErrorState.QuestLogFull);

-----------------------------
 Promise: Then
-----------------------------
Invoked after the Deferred has been resolved, with parameter "x" being the resolved arg.
This is similar to .Done, but with some key differences.
 - .Then which are subscribed will be executed sequentially, if one of them fails, immediately exit (without invoking any others) and invoke .Fail.
 - .Then is capable of changing the promise into a new promise

// Assuming that personService has a method .Get(id) which returns a Promise<Person>

personService.Get(id: 123)
	.Then(x => 
	{
		Debug.Log(string.Format("Person.Name={0}", x.Name));
	});
	
// Changing Promise by Then Example
personService.Get(id: 123)
	.Then(x => 
	{
		Debug.Log(string.Format("Person.Name={0}", x.Name));
		return x.Name; //this will change the promise from Promise<Person> to Promise<string>.
	});

// NEW IN 1.3.0!
// Chaining another promise
_userDataService.Auth(authModel)
	.Then(authResponse =>
	{
		Debug.Log(string.Format("[Then] authResponse={0}", authResponse));
		// Returning another promise!
		return _userDataService.Get(authResponse.UserId);
	})
	.Then(user =>
	{
		Debug.Log(string.Format("[Then] User Result. user={0}", user));
	}
	
-----------------------------
 Promise: Done
-----------------------------
Invoked after the Deferred has been fully resolved, with parameter "x" being the resolved arg.
By fully resolved means, all .Then subscribed has been finished successful.

// Assuming that personService has a method .Get(id) which returns a Promise<Person>

personService.Get(id: 123)
	.Done(x => 
	{
		Debug.Log(string.Format("Person.Name={0}", x.Name));
	});

-----------------------------
 Promise: Fail
-----------------------------
Fail will be invoked in different ways:
 - deferred has been rejected e.g. deferred.Reject()
 - an error has occurred during .Then()
This is very similar to try/catch, were Fail being the catch.

// Assuming that personService has a method .Get(id) which returns a Promise<Person>

// This will catch all Fail's of any type.
personService.Get(id: 123)
	.Fail(x => 
	{		
		Debug.Log(string.Format("An error has occurred. x={0}", x));
	});

	
// This will catch ONLY Fail's which are of specific type: System.Net.HttpStatusCode
personService.Get(id: 123)
	.Fail<System.Net.HttpStatusCode>(x => 
	{		
		Debug.Log(string.Format("An error has occurred. x={0}", x));
	});

-----------------------------
 Promise: Finally
-----------------------------
This is very similar to try/catch/finally, were Finally being the finally, meaning it will be invoked either when a promise is fully resolved (e.g. not when .Then is complete but all the chains are complete) or when it fails.

// Assuming that personService has a method .Get(id) which returns a Promise<Person>

personService.Get(id: 123)
	.Finally(() => 
	{		
		Debug.Log("Promise ends.");
	});

-----------------------------
 Promise: Progress
-----------------------------
Invoked by calling Notify on the Deferred, with parameter "x" being the .Notify arg.

// Assuming that personService has a method .Get(id) which returns a Promise<Person>

personService.Get(id: 123)
	.Progress<string>(x => 
	{
		Debug.Log(string.Format("The progress status={0}", x))
	});

-----------------------------
 Promise Method Chaining
-----------------------------
Promise methods can be chained together

// Assuming that personService has a method .Get(id) which returns a Promise<Person>

personService.Get(id: 123)
	.Then(x => //x is Person
	{
		Debug.Log(string.Format("Person.Name={0}", x.Name));
		return x.Name; //this will change the promise from Promise<Person> to Promise<string>
	})
	.Then(x => //x is string because of the above.
	{
		Debug.Log(string.Format("x={0}", x));		
	})
	.Progress<string>(x => Debug.Log(string.Format("The progress status={0}", x)))
	.Fail<System.Net.HttpStatusCode>(x => Debug.Log(string.Format("An error has occurred. x={0}", x)))
	.Fail(x => Debug.Log(string.Format("An error has occurred. x={0}", x)))
	.Finally(() => Debug.Log("Promise ends."));


-----------------------------
 Promise.All
-----------------------------
Wait for all promises specified to be resolved (parallel) in order for the All Promise to be resolved, if any of them fails, it will immediately reject the promise.

// Assuming that personService has a method .Get(id) which returns a Promise<Person>

var personPromise = personService.Get(id: 123);
var personPromise2 = personService.Get(id: 2);

Promise.All(personPromise, personPromise2)
				.Then(x =>
				      {
					      Debug.Log(string.Format("All Sample [Then] all complete. result. x[0]={0} x[1]={1}", x));
					      return (Person) x[0]; // return first value which is the first person.
				      })


-----------------------------
 Promise.AllSequentially
-----------------------------
Executes a promise function one by one sequentially and resolve after all are complete or reject when one fails.

usage: 
// Assuming that Current.Shutdown and sceneControllerTo.Initialize returns a Promise or Promise<T>
Promise.AllSequentially(Current.Shutdown, sceneControllerTo.Initialize)


=================
 CHANGE LOGS
=================

---------------------------------------
 v1.4.0 - 14/05/2014
---------------------------------------
Change: 
 - Implemented Promise.Reject and Promise.Reject<T> which allows you to simply return a rejected promise.
	usage: return Promise.Reject(PlayerQuestErrorState.QuestLogFull);
 - Implemented Then parameterless.
 - Implemented Promise All Sequentially, like Promise.All but they will run one after the other.
	usage: Promise.AllSequentially(Current.Shutdown, sceneControllerTo.Initialize).
Break Changes:
 - Done has been changed: It will now return void which can be used at the end of the chain.
 - TaskFactory has been renamed to PromiseFactory.
Fix:
 - Fixed an issue on TaskFactory.StartNewDelayed which caused it to never resolve.
 - TaskFactory cancellation is now passed properly to functions from signatures.

---------------------------------------
 v1.3.2 - 23/04/2014
---------------------------------------
 - Then Promise chaining (non generic support).
 - Fixes in Finally when using Promise<T>, and an error occurs it was causing it to crash.

---------------------------------------
 v1.3.1 - 19/03/2014
---------------------------------------
 - Cleaned several Debug.Logs

---------------------------------------
 v1.3.0 - 15/03/2014
---------------------------------------
 - .Then promise chaining - now able to chain other promises in .Then.
 - Included sample for HttpPromise vs TraditionalHttp
 - MoveSample
	- Implemented Sample Move To A and B (chaining)
	- Fixed: Issue when moving to the same location on functionality will be disabled.

---------------------------------------
 v1.2.0 - 06/03/2014
---------------------------------------
 - Implemented Promise<T>.Fail(Action<object>) which was missing to match Promise (non generic)
 - Implemented Promise<T>.Finally(Action) which was missing to match Promise (non generic)
 - TaskFactory:
	- Created StartNewDeferred which will create a new deferred without being automatically resolved. 
	- Recreated methods to ensure both StartNew for Promise<T> and Promise will have exact same functionality and signatures.
	- Refactoring

---------------------------------------
 v1.1.0 - 27/02/2014
---------------------------------------
 - Changed namespace to reflect uPromise properly.
 - Reworked .Fail method to have a better API.
	- Implemented .Fail<TFail> method which listens only to the specified type.
	- .Fail<object> and .Fail are now triggered always when failed.
	- Implemented Reject<TFail>(TFail arg) which allows fail for specified type.

---------------------------------------
 v1.0.0 - 24/02/2014
---------------------------------------
initial release