using Android.App;
using Android.Widget;
using Android.OS;
using Android.Gms.Gcm;
using Android.Gms.Gcm.Iid;
using System;
using Android.Content;

namespace XamarinAndroidSample
{
    [Activity (Label = "PushSharp Client", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        EditText editTextRegistrationId;

        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);

            editTextRegistrationId = FindViewById<EditText> (Resource.Id.editTextRegistrationId);

            // Register for the token refresh, simply to update the UI
            MyRegistrationService.TokenRefreshed += token => {
                RunOnUiThread (() => {
                    editTextRegistrationId.Text = token;
                });
            };

            // Get the last token registered and display immediately
            var p = GetSharedPreferences ("gcmsample", FileCreationMode.Private);
            editTextRegistrationId.Text = p.GetString ("gcm-token", "N/A");

            // Initiate GCM registration to see if a new token is returned
            MyRegistrationService.Register (this);
        }
    }

    [Service]
    public class MyRegistrationService : IntentService
    {
        // This is your 'PROJECT NUMBER' from your Google Developer's Console project
        public const string GCM_SENDER_ID = "YOUR-PROJECT-NUMBER";

        public static event Action<string> TokenRefreshed;

        public static void Register (Context context)
        {
            context.StartService (new Intent (context, typeof(MyRegistrationService)));
        }

        protected override void OnHandleIntent (Intent intent)
        {
            // Get the new token and send to the server
            var instanceID = InstanceID.GetInstance (this);
            var token = instanceID.GetToken (GCM_SENDER_ID, GoogleCloudMessaging.InstanceIdScope);


            var p = GetSharedPreferences ("gcmsample", FileCreationMode.Private);
            var oldToken = p.GetString ("gcm-token", "");

            if (oldToken != token) {
                var pe = p.Edit ();
                pe.PutString ("gcm-token", token);
                pe.Commit ();

                // TODO: You will want to send this token change to your server!

                // Fire the event for any UI subscribed to it
                TokenRefreshed?.Invoke (token);

                Android.Util.Log.Debug ("GCM-SAMPLE", "OnTokenRefresh: {0}", token);
            }
        }
    }

    [Service (Exported=false)]
    [IntentFilter (new [] { InstanceID.IntentFilterAction })]
    public class MyInstanceIDListenerService : InstanceIDListenerService
    {
        public override void OnTokenRefresh ()
        {
            MyRegistrationService.Register (this);
        }
    }

    [Service (Exported=false)]
    [IntentFilter (new [] { GoogleCloudMessaging.IntentFilterActionReceive })]
    public class MyGcmListenerService : GcmListenerService
    {
        const string TAG = "GCM-SAMPLE";

        public override void OnDeletedMessages ()
        {
            base.OnDeletedMessages ();

            Android.Util.Log.Debug (TAG, "Messages Deleted");
        }

        public override void OnMessageReceived (string from, Bundle data)
        {
            var message = data.GetString ("message");
            Android.Util.Log.Debug (TAG, "From: " + from);
            Android.Util.Log.Debug (TAG, "Message: " + message);

            /**
             * Production applications would usually process the message here.
             * Eg: - Syncing with server.
             *     - Store message in local database.
             *     - Update UI.
             */

            /**
            * In some cases it may be useful to show a notification indicating to the user
            * that a message was received.
            */
        }

        public override void OnMessageSent (string msgId)
        {
            base.OnMessageSent (msgId);

            Android.Util.Log.Debug (TAG, "Message Sent: {0}", msgId);
        }

        public override void OnSendError (string msgId, string error)
        {
            base.OnSendError (msgId, error);

            Android.Util.Log.Debug (TAG, "Message Failed: {0} - {1}", msgId, error);
        }
    }
}
