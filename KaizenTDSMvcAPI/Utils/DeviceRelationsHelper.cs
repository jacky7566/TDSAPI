using Dapper;
using KaizenTDSMvcAPI.Models.KaizenTDSClasses;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KaizenTDSMvcAPI.Utils
{
    public class DeviceRelationsHelper
    {
        private static List<DeviceRelationClass> GetDeviceChildRelations(List<string> serialNumbers)
        {
            string sql = string.Format(@"SELECT T.DEVICETYPE, D.* FROM DEVICERELATION D, TESTHEADER T
                        WHERE D.SERIALNUMBER=T.SERIALNUMBER AND D.SERIALNUMBER in ('{0}') ", string.Join("','", serialNumbers.Distinct()));
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                {
                    var list = sqlConn.Query<DeviceRelationClass>(sql).ToList();
                    return list;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static List<DeviceRelationClass> GetDeviceParentRelations(List<string> serialNumbers)
        {
            string sql = string.Format(@"SELECT T.DEVICETYPE, D.* FROM DEVICERELATION D, TESTHEADER T
                        WHERE D.SERIALNUMBER=T.SERIALNUMBER AND D.RELATETOSERIALNUMBER in ('{0}') ", string.Join("','", serialNumbers.Distinct()));
            try
            {
                using (var sqlConn = new OracleConnection(ConnectionHelper.ConnectionInfo.DEFAULTCONNECTIONSTRING))
                {
                    var list = sqlConn.Query<DeviceRelationClass>(sql).ToList();
                    return list;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static List<DeviceRelationClass> RecursiveDeviceRelations(List<string> serialNumbers, List<string> deviceRelationIds, bool recursive)
        {
            var allRoots = new List<DeviceRelationClass>();

            var childs = RecursiveDeviceChildRelations(serialNumbers, deviceRelationIds);//Do Recursive
            var parents = RecursiveDeviceParentRelations(serialNumbers, deviceRelationIds);//Do Recursive

            if (childs.Count() > 0 || parents.Count() > 0)
            {
                allRoots.AddRange(childs);
                allRoots.AddRange(parents);
                if (recursive)
                {
                    var tempSerialNum = allRoots.Select(r => r.RELATETOSERIALNUMBER).ToList();
                    tempSerialNum.AddRange(allRoots.Select(r => r.SERIALNUMBER).ToList());
                    tempSerialNum.Distinct();

                    deviceRelationIds.AddRange(allRoots.Select(r => r.DEVICERELATIONID).ToList());
                    deviceRelationIds.Distinct();
                    //var tempDevicerelationids = allRoots.Select(r => r.DEVICERELATIONID).ToList();

                    allRoots.AddRange(RecursiveDeviceRelations(tempSerialNum, deviceRelationIds, true));
                }                
            }

            return allRoots;
        }


        private static List<DeviceRelationClass> RecursiveDeviceChildRelations(List<string> serialNumbers, List<string> deviceRelationIds)
        {
            var child = GetDeviceChildRelations(serialNumbers).Where(x => deviceRelationIds.Contains(x.DEVICERELATIONID) == false);//Do Recursive
            var tmpChilds = new List<DeviceRelationClass>();

            if (child.Count() > 0)
            {
                tmpChilds.AddRange(child);
                var tempSerialNum = child.Select(r => r.RELATETOSERIALNUMBER).ToList();
                //var tempDevicerelationids = child.Select(r => r.DEVICERELATIONID).ToList();
                deviceRelationIds.AddRange(child.Select(r => r.DEVICERELATIONID).ToList());

                tmpChilds.AddRange(RecursiveDeviceChildRelations(tempSerialNum, deviceRelationIds));
            }
            return tmpChilds;
        }

        private static List<DeviceRelationClass> RecursiveDeviceParentRelations(List<string> serialNumbers, List<string> deviceRelationIds)
        {
            var parent = GetDeviceParentRelations(serialNumbers)
                .Where(x => deviceRelationIds.Contains(x.DEVICERELATIONID) == false);//Do Recursive
            var tmpParents = new List<DeviceRelationClass>();

            if (parent.Count() > 0)
            {
                tmpParents.AddRange(parent);
                var tempSerialNum = parent.Select(r => r.SERIALNUMBER).ToList();
                deviceRelationIds.AddRange(parent.Select(r => r.DEVICERELATIONID).ToList());
                tmpParents.AddRange(RecursiveDeviceParentRelations(tempSerialNum, deviceRelationIds));
            }
            return tmpParents;
        }

    }
}