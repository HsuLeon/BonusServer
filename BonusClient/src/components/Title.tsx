import { memo } from "react";

const Title = () => {
  return (
    <div className="flex justify-start items-center">
      <img className="h-20 w-20" />
      <div className="flex flex-col gap-5 justify-center items-center">
        <p className="text-3xl font-bold">單店彩</p>
      </div>
      <img className="h-20 w-20" />
    </div>
  );
};

const MemoTitle = memo(Title);

export default MemoTitle;
