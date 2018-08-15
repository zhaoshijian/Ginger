﻿#region License
/*
Copyright © 2014-2018 European Support Limited

Licensed under the Apache License, Version 2.0 (the "License")
you may not use this file except in compliance with the License.
You may obtain a copy of the License at 

http://www.apache.org/licenses/LICENSE-2.0 

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS, 
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
See the License for the specific language governing permissions and 
limitations under the License. 
*/
#endregion

using Amdocs.Ginger.Common;
using Amdocs.Ginger.Common.Enums;
using Amdocs.Ginger.Repository;
using System;

namespace Amdocs.Ginger.Common.UIElement
{
    public class ElementLocator : RepositoryItemBase 
    {

        private bool mActive { get; set; }
        public bool Active { get { return mActive; } set { mActive = value; OnPropertyChanged(nameof(Active)); } }

        private eLocateBy mLocateBy;
        [IsSerializedForLocalRepository]
        public eLocateBy LocateBy
        {
            get { return mLocateBy; }
            set { mLocateBy = value; OnPropertyChanged(nameof(LocateBy)); }
        }

        private string mLocateValue { get; set; }
        [IsSerializedForLocalRepository]
        public string LocateValue
        {
            get { return mLocateValue; }
            set { mLocateValue = value; OnPropertyChanged(nameof(LocateValue)); } }

        private string mHelp { get; set; }
        public string Help { get { return mHelp; } set { mHelp = value; OnPropertyChanged(nameof(Help)); } }

        private int? mCount { get; set; }
        public int? Count { get { return mCount; } set { mCount = value; OnPropertyChanged(nameof(Count)); } }

        public override string ItemName { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        //private string mTestStatus { get; set; }
        //public string TestStatus { get { return mTestStatus; } set { mTestStatus = value; OnPropertyChanged(nameof(TestStatus)); } }



        public enum eTestStatus
        {
            Pending,
            Passed,
            Failed
        }

        eTestStatus mTestStatus;
        public eTestStatus TestStatus
        {
            get
            {
                return mTestStatus;
            }
            set
            {
                mTestStatus = value;
                OnPropertyChanged(nameof(TestStatusError));
                OnPropertyChanged(nameof(TestStatusIcon));

            }

        }

        public eImageType TestStatusIcon
        {
            get
            {
                switch (TestStatus)
                {
                    case eTestStatus.Passed:
                        return eImageType.Passed;
                    case eTestStatus.Failed:
                        return eImageType.Failed;
                    case eTestStatus.Pending:
                        return eImageType.Pending;
                    default:
                        return eImageType.Pending;
                }
            }
        }

        //public SolidColorBrush TestStatusIconForeground
        //{
        //    get
        //    {
        //        switch (TestStatus)
        //        {
        //            case eTestStatus.Passed:
        //                return (SolidColorBrush)FindResource("$Green");
        //            case eTestStatus.Failed:
        //                return (SolidColorBrush)FindResource("$Red");
        //            default:
        //                return (SolidColorBrush)FindResource("$Orange");
        //        }

        //    }
        //}

        private string mTestStatusError;
        public string TestStatusError
        {
            get
            {
              return  mTestStatusError;
            }
            set
            {
                mTestStatusError = value;
            }
        }


        //public string TestStatusToolTip
        //{
        //    get
        //    {
        //        switch (TestStatus)
        //        {
        //            case eTestStatus.Failed:
        //                return "$Red";
        //            default:
        //                return "$Orange";
        //        }

        //    }
        //}

        

    }
}
