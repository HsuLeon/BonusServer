import { RecordType } from "@Type/Record";
import { TransferTime } from "@Utils/TransferTime";

interface Props {
  index: number;
  record: RecordType;
}

const transferType = ["斯洛 MEGA", "斯洛 MINI", "CR 無珠"];

const WinnerItem = ({ index, record }: Props) => {
  const oddOrEvenItem =
    index % 2 === 0 ? "bg-[#215669]/85 " : "bg-[#763942]/85 ";

  return (
    <div
      className={`flex justify-center items-center text-white rounded-lg gap-5 font-bold text-3xl px-7 py-5 w-full text-center min-w-[800px] max-w-[1200px] ${oddOrEvenItem} `}
    >
      <div className="flex justify-center items-center gap-5 flex-col xs:flex-row flex-1">
        <div className="flex flex-col items-center text-lg flex-1">
          <p>{TransferTime(record.CreateTime, "Y/M/D")}</p>
          <p>{TransferTime(record.CreateTime, "H:M:S")}</p>
        </div>
        <p className="flex-1">{transferType[record.WinSpinType - 1]}</p>
      </div>

      <div className="flex justify-center items-center gap-5 flex-col-reverse xs:flex-row flex-1">
        <p className="flex-1">{record.Scores}</p>
        <div className={`flex flex-col items-center text-lg flex-1`}>
          <p>{record.MachineName || "無"}</p>
          <p>{record.UserAccount || "無"}</p>
        </div>
        <div className={`flex flex-col items-center text-lg flex-1`}>
          <p className="text-3xl">{record.WebSite}</p>
        </div>
      </div>
    </div>
  );
};

export default WinnerItem;
