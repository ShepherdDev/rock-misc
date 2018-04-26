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
    [DisallowConcurrentExecution]
    public class CleanupScheduledTransactions : IJob
    {
        const int BATCH_SIZE = 100;

        public virtual void Execute( IJobExecutionContext context )
        {
            List<string> messages = new List<string>();

            using ( var rockContext = new RockContext() )
            {
                var txnService = new FinancialScheduledTransactionService( rockContext );
                var checkDate = DateTime.Now.AddDays( -7 );
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
