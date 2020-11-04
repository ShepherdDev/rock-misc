using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace com.shepherdchurch.Misc.Jobs
{
    [IntegerField( "Age In Days", "The number of days old a scheduled transaction must be to be considered for cleanup.", true, 7 )]
    [DisallowConcurrentExecution]
    public class CleanupScheduledTransactions : IJob
    {
        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            List<string> messages = new List<string>();
            var daysBack = dataMap.GetString( "AgeInDays" ).AsIntegerOrNull() ?? 7;

            using ( var rockContext = new RockContext() )
            {
                var txnService = new FinancialScheduledTransactionService( rockContext );
                var checkDate = DateTime.Now.AddDays( -daysBack );
                var txns = txnService
                    .Queryable( "AuthorizedPersonAlias.Person,FinancialGateway" )
                    .Where( t => t.IsActive )
                    .Where( t => !t.NextPaymentDate.HasValue || t.NextPaymentDate.Value < checkDate )
                    .ToList();

                foreach ( var txn in txns )
                {
                    string errorMessage = string.Empty;
                    if ( txnService.GetStatus( txn, out errorMessage ) )
                    {
                        rockContext.SaveChanges();
                    }
                    else
                    {
                        messages.Add( errorMessage );
                    }
                }
            }

            context.Result = string.Join( "\r\n", messages );
        }
    }
}
